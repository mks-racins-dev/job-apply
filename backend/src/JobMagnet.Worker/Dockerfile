FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/JobMagnet.Worker/JobMagnet.Worker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:11.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "JobMagnet.Worker.dll"]
