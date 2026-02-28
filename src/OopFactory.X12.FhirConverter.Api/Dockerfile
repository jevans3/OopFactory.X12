FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY OopFactory.X12.sln .
COPY src/OopFactory.X12.Core/OopFactory.X12.Core.csproj src/OopFactory.X12.Core/
COPY src/OopFactory.X12.Hipaa.Core/OopFactory.X12.Hipaa.Core.csproj src/OopFactory.X12.Hipaa.Core/
COPY src/OopFactory.X12.FhirConverter/OopFactory.X12.FhirConverter.csproj src/OopFactory.X12.FhirConverter/
COPY src/OopFactory.X12.FhirConverter.Api/OopFactory.X12.FhirConverter.Api.csproj src/OopFactory.X12.FhirConverter.Api/

# Restore
RUN dotnet restore src/OopFactory.X12.FhirConverter.Api/OopFactory.X12.FhirConverter.Api.csproj

# Copy everything and build
COPY src/ src/
RUN dotnet publish src/OopFactory.X12.FhirConverter.Api/OopFactory.X12.FhirConverter.Api.csproj \
    -c Release -o /app --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Copy templates (ensure they're available at runtime)
COPY src/OopFactory.X12.FhirConverter/Templates /app/Templates

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "OopFactory.X12.FhirConverter.Api.dll"]
