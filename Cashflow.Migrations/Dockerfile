FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cashflow.Migrations/Cashflow.Migrations.csproj", "Cashflow.Migrations/"]
RUN dotnet restore "./Cashflow.Migrations/Cashflow.Migrations.csproj"
COPY . .
WORKDIR "/src/Cashflow.Migrations"
RUN dotnet build "./Cashflow.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cashflow.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cashflow.Migrations.dll"]