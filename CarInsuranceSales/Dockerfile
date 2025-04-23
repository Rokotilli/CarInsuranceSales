FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CarInsuranceSales/CarInsuranceSales.csproj", "CarInsuranceSales/"]
RUN dotnet restore "./CarInsuranceSales/CarInsuranceSales.csproj"
COPY . .
WORKDIR "/src/CarInsuranceSales"
RUN dotnet build "./CarInsuranceSales.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CarInsuranceSales.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CarInsuranceSales.dll"]