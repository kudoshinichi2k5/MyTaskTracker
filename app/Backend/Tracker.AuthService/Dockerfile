FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
ARG SERVICE_NAME
WORKDIR /app
EXPOSE 8080

COPY publish/ .
ENV SERVICE_DLL="${SERVICE_NAME}.dll"

USER app
ENTRYPOINT dotnet $SERVICE_DLL