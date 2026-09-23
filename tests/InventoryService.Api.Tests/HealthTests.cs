using System.Net;

namespace InventoryService.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class HealthTests(ApiFixture api)
{
    [Fact]
    public async Task Health_WithoutApiKey_ReturnsHealthy()
    {
        var client = api.CreateClientWithoutKey();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
