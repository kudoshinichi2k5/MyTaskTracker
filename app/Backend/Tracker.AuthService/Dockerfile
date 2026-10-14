FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY Tracker.AuthService.csproj ./
RUN dotnet restore Tracker.AuthService.csproj

COPY . .
RUN dotnet publish Tracker.AuthService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime-base
WORKDIR /app
EXPOSE 8080

FROM runtime-base AS runtime-artifact
COPY publish/ .
USER app
ENTRYPOINT ["dotnet", "Tracker.AuthService.dll"]

FROM runtime-base AS runtime
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "Tracker.AuthService.dll"]