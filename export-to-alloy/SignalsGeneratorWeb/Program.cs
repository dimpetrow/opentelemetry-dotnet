using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// // Add OpenTelemetry logging provider by calling the WithLogging extension.
// builder.Services.AddOpenTelemetry()
//     .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
//     .WithLogging(logging =>
//     {
//         // logging
//         //     /* Note: ConsoleExporter is used for demo purpose only. In production
//         //        environment, ConsoleExporter should be replaced with other exporters
//         //        (e.g. OTLP Exporter). */
//         //     .AddConsoleExporter();
//     })
//     .WithMetrics(metrics => metrics
//         .AddAspNetCoreInstrumentation()
//         // .AddConsoleExporter((exporterOptions, metricReaderOptions) =>
//         // {
//         //     metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
//         // })
//     )
//     .WithTracing(tracing => tracing
//         .AddAspNetCoreInstrumentation()
//         //.AddConsoleExporter()
//     )
//     ;

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", (ILogger<Program> logger, IConfiguration config) =>
{
    var otlpEndpoint = config["OTEL_EXPORTER_OTLP_ENDPOINT"];
    
    logger.LogInformation("OTEL_EXPORTER_OTLP_ENDPOINT={endpoint}", otlpEndpoint);
    logger.FoodPriceChanged("artichoke", 9.99);

    return "Hello OTEL";
});

app.Logger.StartingApp();

app.Run();

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Starting the app...")]
    public static partial void StartingApp(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Food `{name}` price changed to `{price}`.")]
    public static partial void FoodPriceChanged(this ILogger logger, string name, double price);
}
