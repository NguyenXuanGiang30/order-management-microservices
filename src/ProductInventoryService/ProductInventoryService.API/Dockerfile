FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SharedContracts/SharedContracts.csproj SharedContracts/
COPY ProductInventoryService/ProductInventoryService.Application/ProductInventoryService.Application.csproj ProductInventoryService/ProductInventoryService.Application/
COPY ProductInventoryService/ProductInventoryService.Infrastructure/ProductInventoryService.Infrastructure.csproj ProductInventoryService/ProductInventoryService.Infrastructure/
COPY ProductInventoryService/ProductInventoryService.API/ProductInventoryService.API.csproj ProductInventoryService/ProductInventoryService.API/

RUN dotnet restore ProductInventoryService/ProductInventoryService.API/ProductInventoryService.API.csproj

COPY . .
RUN dotnet publish ProductInventoryService/ProductInventoryService.API/ProductInventoryService.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductInventoryService.API.dll"]
