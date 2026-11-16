FROM mcr.microsoft.com/dotnet/sdk:11.0.100 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/JobMagnet.Api/JobMagnet.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "JobMagnet.Api.dll"]
