docker run `
    -d `
    -p 3000:3000 `
    --name=grafana `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/grafana/bindmountstorage:/var/lib/grafana `
    -v C:/repos/explore/opentelemetry-dotnet/export-to-alloy/observability/grafana/provisioning:/etc/grafana/provisioning `
    -e "GF_PLUGINS_PREINSTALL=grafana-clock-panel" `
    -e "PROVISIONING_DATASOURCES_LOKI_URL=http://localhost:3100" `
    grafana/grafana-enterprise