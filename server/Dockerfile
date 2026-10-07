# Build: docker build -f server/Dockerfile -t battle-hunter-server .   (contexto = raiz do repositório)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY core/BattleHunter.Core.csproj core/
COPY server/BattleHunter.Server.csproj server/
RUN dotnet restore server/BattleHunter.Server.csproj
COPY core/ core/
COPY server/ server/
RUN dotnet publish server/BattleHunter.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
COPY data/ /app/data/
ENV BH_DATA_DIR=/app/data
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
HEALTHCHECK --interval=30s --timeout=3s CMD wget -qO- http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "BattleHunter.Server.dll"]
