using System.Text.Encodings.Web;
using InventoryService.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace InventoryService.Api.UnitTests;

public sealed class ApiKeyAuthenticationHandlerTests
{
    private const string ValidKey = "unit-test-key-0123456789abcdef";

    [Fact]
    public async Task Authenticate_CorrectKey_Succeeds()
    {
        var result = await AuthenticateAsync(ValidKey);

        Assert.True(result.Succeeded);
        Assert.Equal(ApiKeyDefaults.Scheme, result.Ticket!.AuthenticationScheme);
    }

    [Fact]
    public async Task Authenticate_KeyWithSurroundingWhitespace_Succeeds()
    {
        var result = await AuthenticateAsync($"  {ValidKey}\n");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Authenticate_WrongKey_Fails()
    {
        var result = await AuthenticateAsync("wrong-key-0123456789abcdef");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid API key.", result.Failure!.Message);
    }

    [Fact]
    public async Task Authenticate_KeyWithDifferentCase_Fails()
    {
        var result = await AuthenticateAsync(ValidKey.ToUpperInvariant());

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Authenticate_NoHeader_ReturnsNoResult()
    {
        var result = await AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public async Task Authenticate_EmptyHeader_ReturnsNoResult()
    {
        var result = await AuthenticateAsync("");

        Assert.True(result.None);
    }

    [Fact]
    public async Task Authenticate_HeaderSentTwice_Fails()
    {
        var result = await AuthenticateAsync(ValidKey, ValidKey);

        Assert.False(result.Succeeded);
        Assert.Contains("send exactly one", result.Failure!.Message);
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(params string[] headerValues)
    {
        var handler = new ApiKeyAuthenticationHandler(
            new FixedOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            Options.Create(new ApiKeyOptions { ApiKey = ValidKey }));

        var context = new DefaultHttpContext();
        if (headerValues.Length > 0)
            context.Request.Headers[ApiKeyDefaults.HeaderName] = new StringValues(headerValues);

        await handler.InitializeAsync(
            new AuthenticationScheme(ApiKeyDefaults.Scheme, null, typeof(ApiKeyAuthenticationHandler)),
            context);

        return await handler.AuthenticateAsync();
    }

    private sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
