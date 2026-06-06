FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SharedContracts/SharedContracts.csproj SharedContracts/
COPY OrderSalesService/OrderSalesService.Application/OrderSalesService.Application.csproj OrderSalesService/OrderSalesService.Application/
COPY OrderSalesService/OrderSalesService.Infrastructure/OrderSalesService.Infrastructure.csproj OrderSalesService/OrderSalesService.Infrastructure/
COPY OrderSalesService/OrderSalesService.API/OrderSalesService.API.csproj OrderSalesService/OrderSalesService.API/

RUN dotnet restore OrderSalesService/OrderSalesService.API/OrderSalesService.API.csproj

COPY . .
RUN dotnet publish OrderSalesService/OrderSalesService.API/OrderSalesService.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderSalesService.API.dll"]
