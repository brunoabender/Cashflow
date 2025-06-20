FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cashflow.Operations.Api/src/Cashflow.Operations.Api.csproj", "Cashflow.Operations.Api/src/"]
COPY ["./Cashflow.SharedKernel/Cashflow.SharedKernel.csproj", "Cashflow.SharedKernel/"]
RUN dotnet restore "./Cashflow.Operations.Api/src/Cashflow.Operations.Api.csproj"
COPY . .
WORKDIR "/src/Cashflow.Operations.Api/src"
RUN dotnet build "./Cashflow.Operations.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cashflow.Operations.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cashflow.Operations.Api.dll"]