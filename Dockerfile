FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/InventoryService.Api/InventoryService.Api.csproj src/InventoryService.Api/
RUN dotnet restore src/InventoryService.Api/InventoryService.Api.csproj

COPY src/InventoryService.Api/ src/InventoryService.Api/
RUN dotnet publish src/InventoryService.Api/InventoryService.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false && chmod -R a+rX /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "InventoryService.Api.dll"]
