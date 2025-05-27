# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cashflow.Consolidation.Worker/src/Cashflow.Consolidation.Worker.csproj", "Cashflow.Consolidation.Worker/src/"]
COPY ["Cashflow.SharedKernel/Cashflow.SharedKernel.csproj", "Cashflow.SharedKernel/"]
RUN dotnet restore "./Cashflow.Consolidation.Worker/src/Cashflow.Consolidation.Worker.csproj"
COPY . .
WORKDIR "/src/Cashflow.Consolidation.Worker/src"
RUN dotnet build "./Cashflow.Consolidation.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cashflow.Consolidation.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cashflow.Consolidation.Worker.dll"]