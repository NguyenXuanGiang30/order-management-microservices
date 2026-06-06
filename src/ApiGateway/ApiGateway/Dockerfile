FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ApiGateway/ApiGateway/ApiGateway.csproj ApiGateway/ApiGateway/
RUN dotnet restore ApiGateway/ApiGateway/ApiGateway.csproj

COPY . .
RUN dotnet publish ApiGateway/ApiGateway/ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
