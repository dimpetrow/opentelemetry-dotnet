using AppHost1;

var builder = DistributedApplication.CreateBuilder(args);

var (
    exportTelemetryToAlloyParam,
    exportTelemetryToAlloy
) = builder.GetTelemetryConfiguration();

var web = builder.AddProject<Projects.SignalsGeneratorWeb>("web")
    .WithTelemetryConfiguration(exportTelemetryToAlloyParam);

if (exportTelemetryToAlloy)
{
    var collectorResource = builder.AddGrafanaStack();
    web.WithCollectorReferenceAndScrapeEndpoint(collectorResource);
}

builder.Build().Run();

