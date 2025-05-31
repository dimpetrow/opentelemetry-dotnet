using Microsoft.AspNetCore.Builder;

namespace AppHost1;

public record CollectorResourceAndEndpoint(
    IResourceBuilder<ContainerResource> Collector,
    EndpointReference CollectorEndpointReference);

public record TelemetryConfiguration(
    IResourceBuilder<ParameterResource> ExportTelemetryToAlloyParam,
    bool ExportTelemetryToAlloy);

public static class GrafanaStackExtensions
{
    public static TelemetryConfiguration GetTelemetryConfiguration(this IDistributedApplicationBuilder builder)
    {
        var exportTelemetryToAlloyParam = builder.AddParameter("ExportTelemetryToAlloy");
        var exportTelemetryToAlloy = bool.TryParse(exportTelemetryToAlloyParam.Resource.Value, out var toAlloy) && toAlloy;
        return new TelemetryConfiguration(
            exportTelemetryToAlloyParam,
            exportTelemetryToAlloy);
    }
    
    public static IResourceBuilder<ProjectResource> WithTelemetryConfiguration(
        this IResourceBuilder<ProjectResource> resource,
        IResourceBuilder<ParameterResource> exportTelemetryToAlloyParam) =>
        resource.WithEnvironment("EXPORT_TELEMETRY_TO_ALLOY", exportTelemetryToAlloyParam);

    public static CollectorResourceAndEndpoint AddGrafanaStack(this IDistributedApplicationBuilder builder)
    {
        var bindMountPath = "C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability";
        
        var logsEndpointName = "default"; 
        var logs = builder.AddContainer("logs", "grafana/loki")
                .WithBindMount(Path.Combine(bindMountPath, "loki/bindmountstorage"), "/tmp/loki")
                .WithBindMount(Path.Combine(bindMountPath, "loki/loki-config.yaml"), "/etc/loki/local-config.yaml")
                .WithHttpEndpoint(3100, 3100, logsEndpointName)
                .WithArgs("-config.file=/etc/loki/local-config.yaml")
            ;
        var logsEndpointReference = logs.GetEndpoint(logsEndpointName);

        var tracesServerEndpointName = "server"; 
        var tracesReceiverGrpcEndpointName = "grpc";
        var tracesReceiverHttpEndpointName = "http";
        var traces = builder.AddContainer("traces", "grafana/tempo")
                .WithBindMount(Path.Combine(bindMountPath, "tempo/tempo.yaml"), "/etc/tempo.yaml")
                .WithBindMount(Path.Combine(bindMountPath, "tempo/bindmountstorage"), "/var/tempo")
                .WithHttpEndpoint(3200, 3200, tracesServerEndpointName)
                .WithHttpEndpoint(/*24317, */targetPort: 4317, name: tracesReceiverGrpcEndpointName)
                .WithHttpEndpoint(/*24318, */targetPort: 4318, name: tracesReceiverHttpEndpointName)
                .WithArgs("-config.file=/etc/tempo.yaml", "-config.expand-env=true")
            ;
        var tracesReceiverHttpEndpointReference = traces.GetEndpoint(tracesReceiverHttpEndpointName);
        var tracesServerEndpointReference = traces.GetEndpoint(tracesServerEndpointName);

        var metricsEndpointName = "default";
        var metrics = builder.AddContainer("metrics", "prom/prometheus")
                .WithBindMount(Path.Combine(bindMountPath, "prometheus/bindmountstorage"), "/prometheus/data")
                .WithBindMount(Path.Combine(bindMountPath, "prometheus/prometheus.yml"), "/config/prometheus.yml")
                .WithHttpEndpoint(targetPort: 9090, name: metricsEndpointName)
                .WithArgs("--config.file=/config/prometheus.yml", "--web.enable-remote-write-receiver")
            ;
        var metricsEndpointReference = metrics.GetEndpoint(metricsEndpointName);

        builder.AddContainer("grafana", "grafana/grafana-enterprise")
            .WithEnvironment("PROVISIONING_DATASOURCES_LOKI_URL", logsEndpointReference)
            .WithEnvironment("PROVISIONING_DATASOURCES_TEMPO_URL", tracesServerEndpointReference)
            .WithEnvironment("PROVISIONING_DATASOURCES_PROMETHEUS_URL", metricsEndpointReference)
            .WithBindMount(Path.Combine(bindMountPath, "grafana/bindmountstorage"), "/var/lib/grafana")
            .WithBindMount(Path.Combine(bindMountPath, "grafana/provisioning"), "/etc/grafana/provisioning")
            .WithHttpEndpoint(23001, 3000)
            ;

        var collectorEndpointGrpcName = "grpc";
        // var collectorEndpointHttpName = "http";
        var collector = builder.AddContainer("collector", "grafana/alloy")
                .WithBindMount(Path.Combine(bindMountPath, "alloy/config.alloy"), "/etc/alloy/config.alloy")
                // .WithBindMount("C:/repos/explore/opentelemetry-dotnet/export-to-alloy/tmp/log", "/tmp/app-logs/")
                .WithHttpEndpoint(12345, 12345, name: "ui")
                .WithHttpEndpoint(4317, 4317, name: collectorEndpointGrpcName) // gRPC
                // .WithHttpEndpoint(4318, 4318, name: collectorEndpointHttpName) // HTTP
                .WithArgs(
                    "run", 
                    "--server.http.listen-addr=0.0.0.0:12345", 
                    "--storage.path=/var/lib/alloy/data", 
                    "--stability.level", "experimental",
                    "/etc/alloy/config.alloy"
                )
                .WithEnvironment("PROVISIONING_OTEL_EXPORTER_LOKI_URL", logsEndpointReference)
                .WithEnvironment("PROVISIONING_OTEL_EXPORTER_TEMPO_URL", tracesReceiverHttpEndpointReference /*tracesReceiverGrpcEndpointReference*/)
                .WithEnvironment("PROVISIONING_PROM_REMOTEWRITE_URL", metricsEndpointReference)
            ;
        var collectorEndpointReferenceName = collectorEndpointGrpcName;
        var collectorEndpointReference = collector.GetEndpoint(collectorEndpointReferenceName);
        return new CollectorResourceAndEndpoint(collector, collectorEndpointReference);
    }
    
    public static IResourceBuilder<ProjectResource> WithCollectorReferenceAndScrapeEndpoint(
        this IResourceBuilder<ProjectResource> resource,
        CollectorResourceAndEndpoint collectorResourceAndEndpoint)
    {
        var (collector, endpointReference) = collectorResourceAndEndpoint;
        resource
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", endpointReference)
            .WithReference(endpointReference).WaitFor(collector);
    
        var scrapeEndpoint = resource.GetScrapeEndpoint();
        collector.WithEnvironment("PROVISIONING_PROM_SCRAPE_WEB_URL", scrapeEndpoint);

        return resource;
    }
    
    private static ReferenceExpression GetScrapeEndpoint(this IResourceBuilder<ProjectResource> resourceToScrape)
    {
        var webEndpointReference = resourceToScrape.GetEndpoint("http");
        // https://github.com/dotnet/aspire/discussions/8300#discussioncomment-12623405 -- extremely useful in cases where the default generated url passed via ENVVars is not what the resource expects
        // the example here is for Alloy to scrape metrics with prometheus.scrape it expects addresses to be without the scheme
        var referenceExpression = ReferenceExpression.Create(
            $"{webEndpointReference.Property(EndpointProperty.Host)}:{webEndpointReference.Property(EndpointProperty.Port)}");
        return referenceExpression;
    }
}