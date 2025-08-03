# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GatewayApi/GatewayApi.csproj", "GatewayApi/"]
RUN dotnet restore "./GatewayApi/GatewayApi.csproj"
COPY . .
WORKDIR "/src/GatewayApi"
RUN dotnet build "./GatewayApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GatewayApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN apt-get update && apt-get install -y gosu # Install gosu for user switching
COPY GatewayApi/entrypoint.sh /entrypoint.sh
RUN sed -i -e '1s/^\xEF\xBB\xBF//' /entrypoint.sh # Remove BOM if present
RUN chmod +x /entrypoint.sh # Make the entrypoint script executable
ENTRYPOINT ["/entrypoint.sh"]