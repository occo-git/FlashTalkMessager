#!/bin/sh
set -e

chown -R app:app /app/DataProtection-Keys

if [ "$#" -gt 0 ]; then
  echo "Entrypoint started with arguments: $@"
  exec gosu app dotnet GatewayApi.dll "$@" # Запускаем приложение с переданными аргументами
else
  
  exec gosu app dotnet GatewayApi.dll # Запускаем приложение без аргументов
fi