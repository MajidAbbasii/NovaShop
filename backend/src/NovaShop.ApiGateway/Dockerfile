# Docker configuration for NovaShop API Gateway
# This Dockerfile sets up the YARP-based API Gateway for NovaShop microservices

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY backend/src/NovaShop.ApiGateway/NovaShop.ApiGateway.csproj backend/src/NovaShop.ApiGateway/
RUN dotnet restore "backend/src/NovaShop.ApiGateway/NovaShop.ApiGateway.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/backend/src/NovaShop.ApiGateway"
RUN dotnet publish "NovaShop.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Install curl so the container healthcheck (curl -f http://localhost:5100/health) can run.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

# Copy entrypoint script and make it executable.
# The entrypoint.sh script sets ASPNETCORE_URLS=http://0.0.0.0:$PORT at runtime
# (Docker ENV cannot expand shell variables, so we use the shell entrypoint).
COPY backend/src/NovaShop.ApiGateway/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

ENV DOTNET_SYSTEM_GLOBALIZATION_APARTMENT=1

# EXPOSE documents the default port; Render overrides with $PORT at runtime.
EXPOSE 5100

ENTRYPOINT ["/app/docker-entrypoint.sh"]
