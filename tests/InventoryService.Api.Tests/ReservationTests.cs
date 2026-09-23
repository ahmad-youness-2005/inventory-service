using System.Net;
using System.Net.Http.Json;
using InventoryService.Api.Data;
using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;
using InventoryService.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryService.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class ReservationTests(ApiFixture api)
{
    private static object Order(string orderReference, params (Guid ItemId, int Quantity)[] lines) => new
    {
        orderReference,
        lines = lines.Select(l => new { itemId = l.ItemId, quantity = l.Quantity }).ToArray()
    };

    private static async Task<ReservationResponse> ReadAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ReservationResponse>(ApiFixture.Json))!;

    [Fact]
    public async Task Create_ConcurrentOrdersExceedStock_OnlyAvailableQuantityIsReserved()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 10);

        var responses = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ =>
            client.PostAsJsonAsync("/reservations", Order(ApiFixture.NewOrderReference(), (itemId, 1)))));

        Assert.Equal(10, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(40, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var stock = await api.GetStockAsync(client, itemId);
        Assert.Equal(10, stock.ReservedInPieces);
        Assert.Equal(0, stock.AvailableInPieces);
    }

    [Fact]
    public async Task Create_SameOrderRetried_ReturnsExistingReservation()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 10);
        var order = Order(ApiFixture.NewOrderReference(), (itemId, 4));

        var first = await client.PostAsJsonAsync("/reservations", order);
        var retry = await client.PostAsJsonAsync("/reservations", order);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal((await ReadAsync(first)).Id, (await ReadAsync(retry)).Id);
        Assert.Equal(4, (await api.GetStockAsync(client, itemId)).ReservedInPieces);
    }

    [Fact]
    public async Task Create_SameOrderSentInParallel_ReservesOnce()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 100);
        var order = Order(ApiFixture.NewOrderReference(), (itemId, 5));

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            client.PostAsJsonAsync("/reservations", order)));

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"Got {r.StatusCode}"));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        var ids = await Task.WhenAll(responses.Select(ReadAsync));
        Assert.Single(ids.Select(r => r.Id).Distinct());
        Assert.Equal(5, (await api.GetStockAsync(client, itemId)).ReservedInPieces);
    }

    [Fact]
    public async Task Create_SameOrderWithDifferentLines_ReturnsConflict()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 10);
        var orderReference = ApiFixture.NewOrderReference();

        (await client.PostAsJsonAsync("/reservations", Order(orderReference, (itemId, 2)))).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync("/reservations", Order(orderReference, (itemId, 3)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(2, (await api.GetStockAsync(client, itemId)).ReservedInPieces);
    }

    [Fact]
    public async Task Create_OneLineWithoutEnoughStock_ReservesNothing()
    {
        var client = api.CreateClient();
        var enough = await api.CreateItemWithStockAsync(client, pieces: 10);
        var notEnough = await api.CreateItemWithStockAsync(client, pieces: 1);

        var response = await client.PostAsJsonAsync("/reservations",
            Order(ApiFixture.NewOrderReference(), (enough, 5), (notEnough, 2)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, (await api.GetStockAsync(client, enough)).ReservedInPieces);
        Assert.Equal(0, (await api.GetStockAsync(client, notEnough)).ReservedInPieces);
    }

    [Fact]
    public async Task Confirm_MultiLineReservation_RemovesAllLinesFromStock()
    {
        var client = api.CreateClient();
        var a = await api.CreateItemWithStockAsync(client, pieces: 10);
        var b = await api.CreateItemWithStockAsync(client, pieces: 20);
        var orderReference = ApiFixture.NewOrderReference();

        var created = await ReadAsync(await client.PostAsJsonAsync("/reservations",
            Order(orderReference, (a, 3), (b, 7), (a, 2))));
        Assert.Equal(2, created.Lines.Count);

        var confirm = await client.PostAsync($"/reservations/{created.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal(ReservationStatus.Confirmed, (await ReadAsync(confirm)).Status);

        var stockA = await api.GetStockAsync(client, a);
        var stockB = await api.GetStockAsync(client, b);
        Assert.Equal((5, 0), (stockA.QuantityInPieces, stockA.ReservedInPieces));
        Assert.Equal((13, 0), (stockB.QuantityInPieces, stockB.ReservedInPieces));

        var byOrder = await client.GetFromJsonAsync<List<ReservationResponse>>(
            $"/reservations?orderReference={orderReference}", ApiFixture.Json);
        Assert.Equal(created.Id, Assert.Single(byOrder!).Id);
    }

    [Fact]
    public async Task ConfirmAndRelease_Concurrent_OnlyOneSucceeds()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 10);
        var reservation = await ReadAsync(await client.PostAsJsonAsync("/reservations",
            Order(ApiFixture.NewOrderReference(), (itemId, 4))));

        var results = await Task.WhenAll(
            client.PostAsync($"/reservations/{reservation.Id}/confirm", null),
            client.PostAsync($"/reservations/{reservation.Id}/release", null));

        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);

        var stock = await api.GetStockAsync(client, itemId);
        Assert.Equal(0, stock.ReservedInPieces);
        Assert.True(stock.QuantityInPieces is 6 or 10);
    }

    [Fact]
    public async Task ExpireOverdue_PendingReservation_ReleasesStockAndBlocksConfirm()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 10);
        var orderReference = ApiFixture.NewOrderReference();
        var reservation = await ReadAsync(await client.PostAsJsonAsync("/reservations", Order(orderReference, (itemId, 4))));

        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await db.Reservations
                .Where(r => r.Id == reservation.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));

            var expired = await scope.ServiceProvider.GetRequiredService<ReservationService>().ExpireOverdueAsync(default);
            Assert.True(expired >= 1);
        }

        Assert.Equal(0, (await api.GetStockAsync(client, itemId)).ReservedInPieces);

        var confirm = await client.PostAsync($"/reservations/{reservation.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);

        var again = await client.PostAsJsonAsync("/reservations", Order(orderReference, (itemId, 4)));
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }
}
