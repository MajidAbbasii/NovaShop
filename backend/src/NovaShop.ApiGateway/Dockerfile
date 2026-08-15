# Docker configuration for NovaShop API Gateway
# This Dockerfile sets up the YARP-based API Gateway for NovaShop microservices

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine-amd64 AS base

WORKDIR /app

COPY NovaShop.ApiGateway.csproj .
RUN dotnet restore - DisableParallelization true

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-amd64 AS build
WORKDIR /src

COPY . .
WORKDIR "/src/NovaShop.ApiGateway"
RUN dotnet build -c Release -o /app/build

FROM base AS runtime
WORKDIR /app
COPY --from=build /app/build .

ENV ASPNETCORE_URLS=http://+:5100
ENV DOTNET_SYSTEM_GLOBALIZATION_APARTION=1

EXPOSE 5100
EXPOSE 8080

ENTRYPOINT ["dotnet", "--info", "NovaShop.ApiGateway.dll"]