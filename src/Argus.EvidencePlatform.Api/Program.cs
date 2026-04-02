using Argus.EvidencePlatform.Api.Authentication;
using Argus.EvidencePlatform.Api.Features;
using Argus.EvidencePlatform.Api.Features.Evidence;
using Argus.EvidencePlatform.Api.Features.Screenshots;
using Argus.EvidencePlatform.Application;
using Argus.EvidencePlatform.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RequestDecompression;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddPlatformAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddRequestDecompression();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("default", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddApplication();
builder.Services.AddScoped<IValidator<IngestArtifactForm>, IngestArtifactFormValidator>();
builder.Services.AddScoped<IValidator<IngestScreenshotForm>, IngestScreenshotFormValidator>();
builder.AddInfrastructure();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation())
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
        });
    });

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => IsBadRequestException(exception)
        ? StatusCodes.Status400BadRequest
        : StatusCodes.Status500InternalServerError
});
app.UseWhen(
    context => context.Request.Path.Equals("/api/screenshots", StringComparison.OrdinalIgnoreCase),
    branch =>
    {
        branch.Use(async (context, next) =>
        {
            var contentEncodings = context.Request.Headers.ContentEncoding;
            if (contentEncodings.Count != 1
                || !string.Equals(contentEncodings[0], "gzip", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Content-Encoding"] = ["gzip is required for /api/screenshots."]
                }).ExecuteAsync(context);
                return;
            }

            await next(context);
        });
        branch.UseRequestDecompression();
    });
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready");
app.MapPlatformEndpoints();
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"))
    .ExcludeFromDescription();

await app.RunAsync();

static bool IsBadRequestException(Exception exception)
{
    return EnumerateExceptions(exception).Any(current =>
        current is BadHttpRequestException or InvalidDataException);
}

static IEnumerable<Exception> EnumerateExceptions(Exception exception)
{
    for (var current = exception; current is not null; current = current.InnerException)
    {
        yield return current;
    }
}

public partial class Program
{
}
