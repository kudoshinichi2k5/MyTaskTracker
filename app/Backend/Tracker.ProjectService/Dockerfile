# ============================================================
# Base runtime image
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080


# ============================================================
# Build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG SERVICE_NAME
WORKDIR /src
COPY "${SERVICE_NAME}.csproj" ./
# Restore NuGet packages
RUN dotnet restore "${SERVICE_NAME}.csproj"
# Copy source code
COPY . .
RUN dotnet publish "${SERVICE_NAME}.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false


# ============================================================
# Final runtime image
# ============================================================
FROM base AS final
ARG SERVICE_NAME
WORKDIR /app
# Copy only published application files
COPY --from=build /app/publish .
ENV SERVICE_DLL="${SERVICE_NAME}.dll"
# Run as non-root user
USER app
ENTRYPOINT ["sh", "-c", "exec dotnet \"$SERVICE_DLL\""]