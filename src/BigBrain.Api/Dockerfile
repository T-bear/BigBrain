FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props global.json ./
COPY src/BigBrain.Modules/BigBrain.Modules.csproj src/BigBrain.Modules/
COPY src/BigBrain.Sentinel.Contracts/BigBrain.Sentinel.Contracts.csproj src/BigBrain.Sentinel.Contracts/
COPY src/BigBrain.Api/BigBrain.Api.csproj src/BigBrain.Api/
RUN dotnet restore src/BigBrain.Api/BigBrain.Api.csproj

COPY src/BigBrain.Modules/ src/BigBrain.Modules/
COPY src/BigBrain.Sentinel.Contracts/ src/BigBrain.Sentinel.Contracts/
COPY src/BigBrain.Api/ src/BigBrain.Api/
RUN dotnet publish src/BigBrain.Api/BigBrain.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /data /shopping-data /calendar-data /settings-data && chown $APP_UID /data /shopping-data /calendar-data /settings-data
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "BigBrain.Api.dll"]
