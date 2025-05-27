FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8082
EXPOSE 8084

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cashflow.Reporting.Api/src/Cashflow.Reporting.Api.csproj", "Cashflow.Reporting.Api/src/"]
COPY ["Cashflow.SharedKernel/Cashflow.SharedKernel.csproj", "Cashflow.SharedKernel/"]
RUN dotnet restore "./Cashflow.Reporting.Api/src/Cashflow.Reporting.Api.csproj"
COPY . .
WORKDIR "/src/Cashflow.Reporting.Api/src"
RUN dotnet build "./Cashflow.Reporting.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cashflow.Reporting.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cashflow.Reporting.Api.dll"]