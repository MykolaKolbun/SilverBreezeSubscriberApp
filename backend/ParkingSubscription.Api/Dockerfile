# syntax=docker/dockerfile:1
# Multi-stage build for the ParkingSubscription API (targets linux/arm64 on the Pi).
# Build context must be the `backend/` folder (so Directory.Packages.props and all
# referenced projects are available for the multi-project restore).

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restore layer: copy CPM manifest + all referenced .csproj first for better caching.
COPY Directory.Packages.props ./
COPY ParkingSubscription.Domain/*.csproj ParkingSubscription.Domain/
COPY ParkingSubscription.Application/*.csproj ParkingSubscription.Application/
COPY ParkingSubscription.Infrastructure/*.csproj ParkingSubscription.Infrastructure/
COPY ParkingSubscription.Api/*.csproj ParkingSubscription.Api/
RUN dotnet restore ParkingSubscription.Api/ParkingSubscription.Api.csproj

# Copy the rest of the sources and publish.
COPY . .
RUN dotnet publish ParkingSubscription.Api/ParkingSubscription.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
# curl is used by the docker-compose healthcheck.
RUN apk add --no-cache curl
COPY --from=build /app/publish .

# ASP.NET Core listens on 8080 inside the container by default (ASPNETCORE_HTTP_PORTS).
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ParkingSubscription.Api.dll"]
