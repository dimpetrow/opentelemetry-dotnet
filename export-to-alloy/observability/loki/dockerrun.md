docker run `
    --name loki `
    -d `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/loki/bindmountstorage:/tmp/loki `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/loki:/mnt/config `
    -p 3100:3100 `
    grafana/loki:3.4.1 "-config.file=/mnt/config/loki-config.yaml"