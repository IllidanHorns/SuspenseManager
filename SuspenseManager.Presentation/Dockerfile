# ============================================================
# Multi-stage Dockerfile for SuspenseManager.Presentation
# Build context: repository root (one level above this file)
# Usage: docker build -f SuspenseManager.Presentation/Dockerfile .
# ============================================================

# -- Stage 1: Runtime base image --
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# -- Stage 2: Build --
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files in dependency order for optimal layer caching.
# Docker will skip the restore layer if no .csproj files changed.
COPY ["SuspenseManager.Presentation/SuspenseManager.Presentation.csproj", "SuspenseManager.Presentation/"]
COPY ["SuspenseManager.Application/SuspenseManager.Application.csproj", "SuspenseManager.Application/"]
COPY ["SuspenseManager.Data/SuspenseManager.Data.csproj",               "SuspenseManager.Data/"]
COPY ["SuspenseManager.Models/SuspenseManager.Models.csproj",           "SuspenseManager.Models/"]
COPY ["SuspenseManager.Common/SuspenseManager.Common.csproj",           "SuspenseManager.Common/"]

RUN dotnet restore "SuspenseManager.Presentation/SuspenseManager.Presentation.csproj"

# Copy full source after restore to exploit the cache layer above
COPY . .

WORKDIR "/src/SuspenseManager.Presentation"
RUN dotnet build "SuspenseManager.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# -- Stage 3: Publish --
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SuspenseManager.Presentation.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# -- Stage 4: Final runtime image --
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SuspenseManager.Presentation.dll"]
