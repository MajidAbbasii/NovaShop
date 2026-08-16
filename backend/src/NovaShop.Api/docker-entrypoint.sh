#!/bin/bash
set -e

echo "Starting NovaShop entrypoint script"

# Migrations are handled by EF Core Database.Migrate() at app startup.
# No external migrator service is needed.

cd /app
echo "Starting NovaShop.Api"
exec dotnet NovaShop.Api.dll
