#!/bin/bash
set -e

echo "Starting NovaShop entrypoint script"

# Wait up to 120 seconds for migration flag written by migrator
WAIT_SECONDS=120
SLEEP_INTERVAL=2
ELAPSED=0

while [ $ELAPSED -lt $WAIT_SECONDS ]; do
  if [ -f "/src/.migrations_done" ]; then
    echo "Migration flag detected. Proceeding to start application."
    break
  fi
  echo "Waiting for migration to complete... ($ELAPSED/$WAIT_SECONDS)"
  sleep $SLEEP_INTERVAL
  ELAPSED=$((ELAPSED+SLEEP_INTERVAL))
done

if [ $ELAPSED -ge $WAIT_SECONDS ]; then
  echo "Migration flag not detected after $WAIT_SECONDS seconds. Continuing startup anyway."
fi

cd /app
echo "Starting NovaShop.Api"
exec dotnet NovaShop.Api.dll
