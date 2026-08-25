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

ENV ASPNETCORE_URLS=http://+:5100
ENV DOTNET_SYSTEM_GLOBALIZATION_APARTMENT=1

EXPOSE 5100

ENTRYPOINT ["dotnet", "NovaShop.ApiGateway.dll"]
