using Microsoft.Extensions.Logging;

using System.Diagnostics.Metrics;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

public class Program
{
    /*
    docker run `
        -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/config.alloy:/etc/alloy/config.alloy `
        -p 12345:12345 `
        -p 4317:4317 `
        -p 4318:4318 `
        -d `
        grafana/alloy:latest `
        run --server.http.listen-addr=0.0.0.0:12345 --storage.path=/var/lib/alloy/data --stability.level experimental `
        /etc/alloy/config.alloy
     */
    
    public static async Task Main()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                // metrics.AddOtlpExporter(opt => opt.BatchExportProcessorOptions.)
                metrics.AddMeter("MyCompany.MyProduct.MyLibrary");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource("MyCompany.MyProduct.MyLibrary");
            })
            .UseOtlpExporter();

        builder.Services.AddHostedService<Signals>();
        
        var app = builder.Build();
        await app.RunAsync();
    }
}

public class Signals : BackgroundService
{
    private readonly ILogger<Signals> _logger;
    
    private readonly Counter<long> _myFruitCounter;
    private readonly Gauge<long> _myHeightCounter;
    
    private static readonly ActivitySource MyActivitySource = new("MyCompany.MyProduct.MyLibrary");

    public Signals(
        ILogger<Signals> logger,
        IMeterFactory meterFactory)
    {
        _logger = logger;
        
        var meter = meterFactory.Create("MyCompany.MyProduct.MyLibrary", "1.0");
        _myFruitCounter = meter.CreateCounter<long>("MyFruitCounter");
        _myHeightCounter = meter.CreateGauge<long>("MyHeightCounter");
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SignalLogs();
        SignalMetrics();
        SignalTraces();

        return Task.CompletedTask;
    }

    private static void SignalTraces()
    {
        using (var activity = MyActivitySource.StartActivity("SayHello", ActivityKind.Client))
        {
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
    }

    private void SignalMetrics()
    {
        _myFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
        _myFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
        _myFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
        _myFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
        _myFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
        _myFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));
        
        _myHeightCounter.Record(178, new ("name", "John"), new("age", "18"));
        _myHeightCounter.Record(156, new ("name", "Susan"), new("age", "20"));
        _myHeightCounter.Record(157, new ("name", "Susan"), new("age", "20"));
        _myHeightCounter.Record(177, new ("name", "John"), new("age", "18"));
        _myHeightCounter.Record(159, new ("name", "Susan"), new("age", "20"));
    }

    private void SignalLogs()
    {
        _logger.FoodPriceChanged("artichoke", 9.99);

        _logger.FoodRecallNotice(
            brandName: "Contoso",
            productDescription: "Salads",
            productType: "Food & Beverages",
            recallReasonDescription: "due to a possible health risk from Listeria monocytogenes",
            companyName: "Contoso Fresh Vegetables, Inc.");
    }
}

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Food `{name}` price changed to `{price}`.")]
    public static partial void FoodPriceChanged(this ILogger logger, string name, double price);

    [LoggerMessage(LogLevel.Critical, "A `{productType}` recall notice was published for `{brandName} {productDescription}` produced by `{companyName}` ({recallReasonDescription}).")]
    public static partial void FoodRecallNotice(
        this ILogger logger,
        string brandName,
        string productDescription,
        string productType,
        string recallReasonDescription,
        string companyName);
}
