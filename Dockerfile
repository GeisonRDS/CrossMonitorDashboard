# Stage 1: Build Vue frontend
FROM node:20-alpine AS vue-build
WORKDIR /src
COPY src/CrossMonitorDashboard.Web/package*.json ./
RUN npm ci
COPY src/CrossMonitorDashboard.Web/ ./
RUN npm run build

# Stage 2: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /src
COPY . ./
RUN dotnet restore src/CrossMonitorDashboard.Api/CrossMonitorDashboard.Api.csproj
RUN dotnet publish src/CrossMonitorDashboard.Api/CrossMonitorDashboard.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy Vue dist to be served by .NET backend
COPY --from=vue-build /src/dist ./wwwroot

# Copy .NET publish output
COPY --from=dotnet-build /app/publish ./

# Copy default example config
COPY config/dashboard.example.json ./config/dashboard.example.json

ENV ASPNETCORE_URLS=http://+:9580
ENV DASHBOARD_WEB_ROOT=/app/wwwroot
ENV DASHBOARD_CONFIG_PATH=/app/config/dashboard.json

EXPOSE 9580

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl --fail --silent http://localhost:9580/health > /dev/null || exit 1

ENTRYPOINT ["dotnet", "CrossMonitorDashboard.Api.dll"]
