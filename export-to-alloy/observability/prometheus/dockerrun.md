docker run `
    --name prometheus `
    -d `
    -p 9090:9090 `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/prometheus/bindmountstorage:/prometheus/data `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/prometheus/prometheus.yml:/config/prometheus.yml `
    prom/prometheus `
    --config.file=/config/prometheus.yml