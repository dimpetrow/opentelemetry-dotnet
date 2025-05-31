docker run `
    --name tempo `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/tempo/bindmountstorage:/var/tempo `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/tempo/tempo.yaml:/etc/tempo.yaml `
    -p 3200:3200 `
    -p 4317:4317 `
    -d `
    grafana/tempo `
    "-config.file=/etc/tempo.yaml" `
    "-config.expand-env=true"