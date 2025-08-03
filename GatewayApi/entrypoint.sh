#!/bin/sh
chown -R app:app /app/DataProtection-Keys
exec dotnet GatewayApi.dll