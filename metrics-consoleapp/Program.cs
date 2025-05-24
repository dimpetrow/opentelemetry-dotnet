using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

public class Program
{
    private static readonly Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
    private static readonly Counter<long> MyFruitCounter = MyMeter.CreateCounter<long>("MyFruitCounter");
    private static readonly Gauge<long> MyHeightCounter = MyMeter.CreateGauge<long>("MyHeightCounter");

    public static void Main()
    {
        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("MyCompany.MyProduct.MyLibrary")
            .AddConsoleExporter()
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
}