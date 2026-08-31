# syntax=docker/dockerfile:1
# Multi-stage build for the public Web client (targets linux/arm64 on the Pi).
# Build context must be the `backend/` folder.
#
# This is a thin server-rendered client (Razor Pages) that talks to the API over
# HTTP — no database, no project references. Api base URL comes from Api__BaseUrl.

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY ParkingSubscription.Web/*.csproj ParkingSubscription.Web/
RUN dotnet restore ParkingSubscription.Web/ParkingSubscription.Web.csproj

COPY . .
RUN dotnet publish ParkingSubscription.Web/ParkingSubscription.Web.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache curl icu-libs
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "ParkingSubscription.Web.dll"]
