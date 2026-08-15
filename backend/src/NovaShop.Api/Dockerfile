FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY backend/src/NovaShop.Api/NovaShop.Api.csproj backend/src/NovaShop.Api/
COPY backend/src/NovaShop.Application/NovaShop.Application.csproj backend/src/NovaShop.Application/
COPY backend/src/NovaShop.Domain/NovaShop.Domain.csproj backend/src/NovaShop.Domain/
COPY backend/src/NovaShop.Common/NovaShop.Common.csproj backend/src/NovaShop.Common/
COPY backend/src/NovaShop.Infrastructure/NovaShop.Infrastructure.csproj backend/src/NovaShop.Infrastructure/

RUN dotnet restore "backend/src/NovaShop.Api/NovaShop.Api.csproj"

# copy everything else and publish
COPY . .
WORKDIR /src/backend/src/NovaShop.Api
RUN dotnet publish "NovaShop.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Copy entrypoint script and make it executable
COPY backend/src/NovaShop.Api/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

ENTRYPOINT ["/app/docker-entrypoint.sh"]
