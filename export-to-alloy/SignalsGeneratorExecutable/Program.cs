using Microsoft.Extensions.Logging;

using System.Diagnostics.Metrics;
using System.Diagnostics;

using OpenTelemetry;
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
    
    private static readonly Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
    private static readonly Counter<long> MyFruitCounter = MyMeter.CreateCounter<long>("MyFruitCounter");
    private static readonly Gauge<long> MyHeightCounter = MyMeter.CreateGauge<long>("MyHeightCounter");

    private static readonly ActivitySource MyActivitySource = new("MyCompany.MyProduct.MyLibrary");

    public static void Main()
    {
        Console.WriteLine("start");
        
        SignalLogs();
        SignalMetrics();
        SignalTraces();
        
        Console.WriteLine("end");
    }

    private static void SignalLogs()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(logging =>
                {
                    logging.AddOtlpExporter((exporterOptions) =>
                    {
                        exporterOptions.Endpoint = new Uri("http://localhost:4317");
                    });
                    // logging.AddConsoleExporter();
                });
        });
        
        var logger = loggerFactory.CreateLogger<Program>();

        // trigger signals
        logger.FoodPriceChanged("artichoke", 9.99);
        
        logger.FoodRecallNotice(
            brandName: "Contoso",
            productDescription: "Salads",
            productType: "Food & Beverages",
            recallReasonDescription: "due to a possible health risk from Listeria monocytogenes",
            companyName: "Contoso Fresh Vegetables, Inc.");

        // Dispose logger factory before the application ends.
        // This will flush the remaining logs and shutdown the logging pipeline.
        loggerFactory.Dispose();
    }

    private static void SignalMetrics()
    {
        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("MyCompany.MyProduct.MyLibrary")
            .AddOtlpExporter((exporterOptions) =>
            {
                exporterOptions.Endpoint = new Uri("http://localhost:4317");
            })
            // .AddConsoleExporter()
            .Build();

        // In this example, we have low cardinality which is below the 2000
        // default limit. If you have high cardinality, you need to set the
        // cardinality limit properly.
        MyFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
        MyFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
        MyFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
        MyFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
        MyFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
        MyFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));
        
        MyHeightCounter.Record(178, new ("name", "John"), new("age", "18"));
        MyHeightCounter.Record(156, new ("name", "Susan"), new("age", "20"));
        MyHeightCounter.Record(157, new ("name", "Susan"), new("age", "20"));
        MyHeightCounter.Record(177, new ("name", "John"), new("age", "18"));
        MyHeightCounter.Record(159, new ("name", "Susan"), new("age", "20"));
        
        // Dispose meter provider before the application ends.
        // This will flush the remaining metrics and shutdown the metrics pipeline.
        meterProvider.Dispose();
    }

    private static void SignalTraces()
    {
        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("MyCompany.MyProduct.MyLibrary")
            .AddOtlpExporter((exporterOptions) =>
            {
                exporterOptions.Endpoint = new Uri("http://localhost:4317");
            })
            // .AddConsoleExporter()
            .Build();

        using (var activity = MyActivitySource.StartActivity("SayHello", ActivityKind.Client))
        {
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        
        // Dispose tracer provider before the application ends.
        // This will flush the remaining spans and shutdown the tracing pipeline.
        tracerProvider.Dispose();
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
