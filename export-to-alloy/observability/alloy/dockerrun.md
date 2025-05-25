docker run `
    --name=alloy `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/alloy/config.alloy:/etc/alloy/config.alloy `
    -p 12345:12345 `
    -p 4317:4317 `
    -p 4318:4318 `
    -e "PROVISIONING_OTEL_EXPORTER_LOKI_URL=http://localhost:3100" `
    -d `
    grafana/alloy:latest `
    run --server.http.listen-addr=0.0.0.0:12345 --storage.path=/var/lib/alloy/data --stability.level=experimental `
    /etc/alloy/config.alloy