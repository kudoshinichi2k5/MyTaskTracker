FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY Tracker.ProjectService.csproj ./
RUN dotnet restore Tracker.ProjectService.csproj

COPY . .
RUN dotnet publish Tracker.ProjectService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime-base
WORKDIR /app
EXPOSE 8080

FROM runtime-base AS runtime-artifact
COPY publish/ .
USER app
ENTRYPOINT ["dotnet", "Tracker.ProjectService.dll"]

FROM runtime-base AS runtime
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "Tracker.ProjectService.dll"]