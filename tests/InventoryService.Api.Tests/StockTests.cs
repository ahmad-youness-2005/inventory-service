using System.Net;
using System.Net.Http.Json;
using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;

namespace InventoryService.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class StockTests(ApiFixture api)
{
    [Fact]
    public async Task StockIn_WithUnit_ConvertsToPieces()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 0);
        var boxId = await api.CreateUomAsync(client);
        (await client.PostAsJsonAsync($"/items/{itemId}/uoms", new { uomId = boxId, quantityPerUnit = 24 }))
            .EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync($"/items/{itemId}/stock/in", new { uomId = boxId, quantity = 10 }))
            .EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/items/{itemId}/stock/out", new { quantity = 3 }))
            .EnsureSuccessStatusCode();

        var stock = await api.GetStockAsync(client, itemId);
        Assert.Equal(237, stock.QuantityInPieces);
        var box = Assert.Single(stock.Units);
        Assert.Equal(9, box.FullUnits);
        Assert.Equal(21, box.RemainingPieces);
    }

    [Fact]
    public async Task StockOut_Concurrent_NeverGoesBelowZero()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 10);

        var responses = await Task.WhenAll(Enumerable.Range(0, 30).Select(_ =>
            client.PostAsJsonAsync($"/items/{itemId}/stock/out", new { quantity = 1 })));

        Assert.Equal(10, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(20, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(0, (await api.GetStockAsync(client, itemId)).QuantityInPieces);
    }

    [Fact]
    public async Task StockOut_ReservedStock_ReturnsConflict()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 10);

        (await client.PostAsJsonAsync("/reservations", new
        {
            orderReference = ApiFixture.NewOrderReference(),
            lines = new[] { new { itemId, quantity = 8 } }
        })).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/items/{itemId}/stock/out", new { quantity = 3 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task History_AfterStockChanges_RecordsEveryMovement()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 0);
        var boxId = await api.CreateUomAsync(client);
        (await client.PostAsJsonAsync($"/items/{itemId}/uoms", new { uomId = boxId, quantityPerUnit = 24 }))
            .EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync($"/items/{itemId}/stock/in", new { uomId = boxId, quantity = 2, reason = "Supplier delivery" }))
            .EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/items/{itemId}/stock/out", new { quantity = 5, reason = "Damaged" }))
            .EnsureSuccessStatusCode();

        var reserve = await client.PostAsJsonAsync("/reservations", new
        {
            orderReference = ApiFixture.NewOrderReference(),
            lines = new[] { new { itemId, quantity = 10 } }
        });
        reserve.EnsureSuccessStatusCode();
        var reservation = (await reserve.Content.ReadFromJsonAsync<ReservationResponse>(ApiFixture.Json))!;
        (await client.PostAsync($"/reservations/{reservation.Id}/confirm", null)).EnsureSuccessStatusCode();

        var history = (await client.GetFromJsonAsync<PagedResponse<StockMovementResponse>>(
            $"/items/{itemId}/stock/history", ApiFixture.Json))!;

        Assert.Equal(4, history.TotalCount);
        Assert.Equal(
            new[] { StockMovementType.Confirmed, StockMovementType.Reserved, StockMovementType.StockOut, StockMovementType.StockIn },
            history.Items.Select(m => m.Type));

        var stockIn = history.Items[3];
        Assert.Equal((48, 0, 48, 0), (stockIn.QuantityChange, stockIn.ReservedChange, stockIn.QuantityAfter, stockIn.ReservedAfter));
        Assert.Equal((boxId, 2, "Supplier delivery"), (stockIn.UomId!.Value, stockIn.UnitQuantity!.Value, stockIn.Reason!));

        var stockOut = history.Items[2];
        Assert.Equal((-5, 43, "Damaged"), (stockOut.QuantityChange, stockOut.QuantityAfter, stockOut.Reason!));

        var reserved = history.Items[1];
        Assert.Equal((0, 10, 43, 10), (reserved.QuantityChange, reserved.ReservedChange, reserved.QuantityAfter, reserved.ReservedAfter));
        Assert.Equal(reservation.Id, reserved.ReservationId);

        var confirmed = history.Items[0];
        Assert.Equal((-10, -10, 33, 0), (confirmed.QuantityChange, confirmed.ReservedChange, confirmed.QuantityAfter, confirmed.ReservedAfter));
        Assert.Equal(reservation.Id, confirmed.ReservationId);

        var stock = await api.GetStockAsync(client, itemId);
        Assert.Equal(stock.QuantityInPieces, history.Items.Sum(m => m.QuantityChange));
        Assert.Equal(stock.ReservedInPieces, history.Items.Sum(m => m.ReservedChange));
    }

    [Fact]
    public async Task History_RejectedStockOut_IsNotRecorded()
    {
        var client = api.CreateClient();
        var itemId = await api.CreateItemWithStockAsync(client, pieces: 2);

        var response = await client.PostAsJsonAsync($"/items/{itemId}/stock/out", new { quantity = 3 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var history = (await client.GetFromJsonAsync<PagedResponse<StockMovementResponse>>(
            $"/items/{itemId}/stock/history", ApiFixture.Json))!;
        Assert.Equal(StockMovementType.StockIn, Assert.Single(history.Items).Type);
    }

    [Fact]
    public async Task GetItems_PageOutOfRange_ReturnsBadRequest()
    {
        var client = api.CreateClient();

        var response = await client.GetAsync("/items?page=2147483647&pageSize=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
