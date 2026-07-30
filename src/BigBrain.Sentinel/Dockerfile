FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props global.json ./
COPY src/BigBrain.Sentinel.Contracts/BigBrain.Sentinel.Contracts.csproj src/BigBrain.Sentinel.Contracts/
COPY src/BigBrain.Sentinel/BigBrain.Sentinel.csproj src/BigBrain.Sentinel/
RUN dotnet restore src/BigBrain.Sentinel/BigBrain.Sentinel.csproj

COPY src/BigBrain.Sentinel.Contracts/ src/BigBrain.Sentinel.Contracts/
COPY src/BigBrain.Sentinel/ src/BigBrain.Sentinel/
RUN dotnet publish src/BigBrain.Sentinel/BigBrain.Sentinel.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
RUN install -d -o $APP_UID -g $APP_UID /run/bigbrain
USER $APP_UID
ENTRYPOINT ["dotnet", "BigBrain.Sentinel.dll"]
