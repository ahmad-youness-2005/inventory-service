using System.Text.Json.Serialization;
using InventoryService.Api.Auth;
using InventoryService.Api.Data;
using InventoryService.Api.Errors;
using InventoryService.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException(
        "Connection string 'InventoryDb' is missing. See 'Connect to Supabase' in README.md.");

builder.Services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddOptions<ReservationOptions>()
    .BindConfiguration(ReservationOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<UomService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<ItemUomService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ReservationService>();

builder.Services.AddHostedService<ReservationExpiryService>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var authEnabled = builder.Configuration.GetValue("Auth:Enabled", true);

if (authEnabled)
{
    builder.Services.AddOptions<ApiKeyOptions>()
        .BindConfiguration(ApiKeyOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddAuthentication(ApiKeyDefaults.Scheme)
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.Scheme, null);
}

builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    if (authEnabled)
        options.AddDocumentTransformer(AddApiKeySecurity);
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Inventory Service v1");
        options.EnablePersistAuthorization();
    });
}

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers().RequireAuthorization();
}
else
{
    app.Logger.LogWarning("API key authentication is disabled (Auth:Enabled = false). Every endpoint is open.");
    app.MapControllers();
}

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });

app.Run();

static Task AddApiKeySecurity(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes[ApiKeyDefaults.Scheme] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = ApiKeyDefaults.HeaderName
    };

    document.Security ??= [];
    document.Security.Add(new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(ApiKeyDefaults.Scheme, document)] = []
    });

    return Task.CompletedTask;
}

public partial class Program { }
