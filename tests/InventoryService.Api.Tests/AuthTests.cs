using System.Net;
using InventoryService.Api.Auth;

namespace InventoryService.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests(ApiFixture api)
{
    [Fact]
    public async Task Request_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = api.CreateClientWithoutKey();

        var response = await client.GetAsync("/uoms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithInvalidApiKey_ReturnsUnauthorized()
    {
        var client = api.CreateClientWithoutKey();
        client.DefaultRequestHeaders.Add(ApiKeyDefaults.HeaderName, "wrong-key-0123456789abcdef");

        var response = await client.GetAsync("/uoms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithValidApiKey_ReturnsOk()
    {
        var client = api.CreateClient();

        var response = await client.GetAsync("/uoms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
