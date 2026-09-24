using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace InventoryService.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class HealthTests(ApiFixture api)
{
    [Fact]
    public async Task Health_WithoutApiKey_ReturnsHealthyJson()
    {
        var client = api.CreateClientWithoutKey();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());

        var database = Assert.Single(body.GetProperty("checks").EnumerateArray());
        Assert.Equal("database", database.GetProperty("name").GetString());
        Assert.Equal("Healthy", database.GetProperty("status").GetString());
    }
}
