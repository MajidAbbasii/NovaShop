#!/bin/bash
set -e
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-5250}"
exec dotnet NovaShop.ApiGateway.dll
