FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SharedContracts/SharedContracts.csproj SharedContracts/
COPY UserReportService/UserReportService.Application/UserReportService.Application.csproj UserReportService/UserReportService.Application/
COPY UserReportService/UserReportService.Infrastructure/UserReportService.Infrastructure.csproj UserReportService/UserReportService.Infrastructure/
COPY UserReportService/UserReportService.API/UserReportService.API.csproj UserReportService/UserReportService.API/

RUN dotnet restore UserReportService/UserReportService.API/UserReportService.API.csproj

COPY . .
RUN dotnet publish UserReportService/UserReportService.API/UserReportService.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserReportService.API.dll"]
