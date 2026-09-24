using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InventoryService.Api.Data;

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var response = new HealthResponse(
            report.Status.ToString(),
            Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            report.Entries
                .Select(e => new HealthCheckEntry(
                    e.Key,
                    e.Value.Status.ToString(),
                    e.Value.Description,
                    Math.Round(e.Value.Duration.TotalMilliseconds, 1)))
                .ToList());

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private sealed record HealthResponse(string Status, double TotalDurationMs, IReadOnlyList<HealthCheckEntry> Checks);

    private sealed record HealthCheckEntry(string Name, string Status, string? Description, double DurationMs);
}
