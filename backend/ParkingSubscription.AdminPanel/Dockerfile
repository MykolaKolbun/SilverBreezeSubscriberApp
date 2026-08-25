# syntax=docker/dockerfile:1
# Multi-stage build for the Admin Panel (targets linux/arm64 on the Pi).
# Build context must be the `backend/` folder.

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY ParkingSubscription.Domain/*.csproj ParkingSubscription.Domain/
COPY ParkingSubscription.Application/*.csproj ParkingSubscription.Application/
COPY ParkingSubscription.Infrastructure/*.csproj ParkingSubscription.Infrastructure/
COPY ParkingSubscription.AdminPanel/*.csproj ParkingSubscription.AdminPanel/
RUN dotnet restore ParkingSubscription.AdminPanel/ParkingSubscription.AdminPanel.csproj

COPY . .
RUN dotnet publish ParkingSubscription.AdminPanel/ParkingSubscription.AdminPanel.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache curl icu-libs
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "ParkingSubscription.AdminPanel.dll"]
