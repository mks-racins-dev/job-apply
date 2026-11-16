FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/JobMagnet.Worker/JobMagnet.Worker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0.12
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "JobMagnet.Worker.dll"]
