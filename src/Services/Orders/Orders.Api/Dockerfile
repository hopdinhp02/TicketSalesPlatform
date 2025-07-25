# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Orders/Orders.Api/TicketFlow.Orders.Api.csproj", "src/Services/Orders/Orders.Api/"]
COPY ["src/Services/Orders/Orders.Application/TicketFlow.Orders.Application.csproj", "src/Services/Orders/Orders.Application/"]
COPY ["src/Services/Orders/Orders.Domain/TicketFlow.Orders.Domain.csproj", "src/Services/Orders/Orders.Domain/"]
COPY ["src/BuildingBlocks/SharedKernel/SharedKernel.csproj", "src/BuildingBlocks/SharedKernel/"]
COPY ["src/BuildingBlocks/IntegrationEvents/TicketFlow.IntegrationEvents.csproj", "src/BuildingBlocks/IntegrationEvents/"]
COPY ["src/Services/Orders/Orders.Infrastructure/TicketFlow.Orders.Infrastructure.csproj", "src/Services/Orders/Orders.Infrastructure/"]
RUN dotnet restore "./src/Services/Orders/Orders.Api/TicketFlow.Orders.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Orders/Orders.Api"
RUN dotnet build "./TicketFlow.Orders.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TicketFlow.Orders.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketFlow.Orders.Api.dll"]