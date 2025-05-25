using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// var debugView = (builder.Configuration as IConfigurationRoot).GetDebugView();
//
// Console.WriteLine(debugView);

var logsEndpointName = "http"; 
var logs = builder.AddContainer("logs", "grafana/loki")
    .WithBindMount("C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/loki/loki-config.yaml",
        "/etc/loki/local-config.yaml")
    .WithArgs("-config.file=/etc/loki/local-config.yaml")
    .WithHttpEndpoint(3100, 3100, logsEndpointName)
    ;
var logsEndpointReference = logs.GetEndpoint(logsEndpointName);

builder.AddContainer("grafana", "grafana/grafana-enterprise")
        .WithEnvironment("PROVISIONING_DATASOURCES_LOKI_URL", logsEndpointReference)
        // .WithEnvironment("GF_PATHS_CONFIG", "/auth.ini")
        // .WithEnvironment("GF_FEATURE_TOGGLES_ENABLE", "accessControlOnCall")
        // .WithEnvironment("GF_INSTALL_PLUGINS", "https://storage.googleapis.com/integration-artifacts/grafana-lokiexplore-app/grafana-lokiexplore-app-latest.zip;grafana-lokiexplore-app")
        .WithBindMount("C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/grafana/bindmountstorage", "/var/lib/grafana")
        .WithBindMount("C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/grafana/provisioning", "/etc/grafana/provisioning")
        // .WithBindMount("C:/repos/explore/opentelemetry-dotnet/export-to-alloy/grafana/auth.ini", "/auth.ini")
        .WithHttpEndpoint(23001, 3000)
    ;

var collectorEndpointGrpcName = "grpc";
// var collectorEndpointHttpName = "http";
var collector = builder.AddContainer("collector", "grafana/alloy")
    .WithBindMount("C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/alloy/config.alloy", "/etc/alloy/config.alloy")
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
    ;
var collectorEndpointReferenceName = collectorEndpointGrpcName;
var collectorEndpointReference = collector.GetEndpoint(collectorEndpointReferenceName);

builder.AddProject<Projects.SignalsGeneratorWeb>("web")
    .WithReference(collectorEndpointReference)
    .WithEnvironment(envCtx =>
    {
        // var collectorEndpointKey = $"services__collector__{collectorEndpointReferenceName}__0";
        // var collectorEndpoint = envCtx.EnvironmentVariables.TryGetValue(collectorEndpointKey, out var url)
        //     ? url
        //     : throw new Exception(collectorEndpointKey);
        // envCtx.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorEndpoint;
        envCtx.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorEndpointReference;
        
        // envCtx.EnvironmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/json";
        var ok = envCtx.EnvironmentVariables.Remove("OTEL_EXPORTER_OTLP_HEADERS");

        var logger = envCtx.ExecutionContext.ServiceProvider.GetRequiredService<ILogger<AppHost1>>();
        logger.LogInformation("OTELHeaders removed: {ok}", ok);
    })
    ;

builder.Build().Run();
