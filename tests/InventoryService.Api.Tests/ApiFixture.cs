using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryService.Api.Auth;
using InventoryService.Api.Data;
using InventoryService.Api.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace InventoryService.Api.Tests;

public sealed class ApiFixture : IAsyncLifetime
{
    public const string ApiKey = "test-api-key-0123456789abcdef";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:InventoryDb", _database.GetConnectionString());
            builder.UseSetting("Auth:ApiKey", ApiKey);
        });

        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    public HttpClient CreateClient()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyDefaults.HeaderName, ApiKey);
        return client;
    }

    public HttpClient CreateClientWithoutKey() => Factory.CreateClient();

    public async Task<Guid> CreateItemWithStockAsync(HttpClient client, int pieces)
    {
        var sku = "T-" + Guid.NewGuid().ToString("N")[..12];
        var response = await client.PostAsJsonAsync("/items", new { sku, name = "Test item " + sku });
        response.EnsureSuccessStatusCode();
        var item = (await response.Content.ReadFromJsonAsync<ItemResponse>(Json))!;

        if (pieces > 0)
            (await client.PostAsJsonAsync($"/items/{item.Id}/stock/in", new { quantity = pieces })).EnsureSuccessStatusCode();

        return item.Id;
    }

    public async Task<Guid> CreateUomAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/uoms", new { name = "U" + Guid.NewGuid().ToString("N")[..12] });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UomResponse>(Json))!.Id;
    }

    public async Task<StockResponse> GetStockAsync(HttpClient client, Guid itemId) =>
        (await client.GetFromJsonAsync<StockResponse>($"/items/{itemId}/stock", Json))!;

    public static string NewOrderReference() => "ORDER-" + Guid.NewGuid().ToString("N")[..12];
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
