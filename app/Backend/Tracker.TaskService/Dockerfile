FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY Tracker.TaskService.csproj ./
RUN dotnet restore Tracker.TaskService.csproj

COPY . .
RUN dotnet publish Tracker.TaskService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime-base
WORKDIR /app
EXPOSE 8080

FROM runtime-base AS runtime-artifact
COPY publish/ .
USER app
ENTRYPOINT ["dotnet", "Tracker.TaskService.dll"]

FROM runtime-base AS runtime
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "Tracker.TaskService.dll"]