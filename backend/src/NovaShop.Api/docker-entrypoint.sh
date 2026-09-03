#!/bin/bash
set -e

echo "Starting NovaShop entrypoint script"

# Migrations are handled by EF Core Database.Migrate() at app startup.
# No external migrator service is needed.

cd /app
# Bind to 0.0.0.0 on the PORT Render assigns (or 5000 as fallback).
# Must use shell expansion here: Docker ENV does not expand $PORT at runtime,
# and ASP.NET Core defaults to localhost-only binding when ASPNETCORE_URLS
# is unset, which causes Render's port scan to time out.
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-5000}"
echo "Starting NovaShop.Api on $ASPNETCORE_URLS"
exec dotnet NovaShop.Api.dll
