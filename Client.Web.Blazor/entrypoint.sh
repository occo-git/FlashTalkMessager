#!/bin/sh
chown -R app:app /app/DataProtection-Keys
exec gosu app dotnet Client.Web.Blazor.dll