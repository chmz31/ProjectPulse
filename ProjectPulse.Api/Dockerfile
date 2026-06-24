FROM mcr.microsoft.com/dotnet/sdk:8.0.422 AS build
WORKDIR /src

COPY global.json ProjectPulse.sln ./
COPY ProjectPulse.Api/ProjectPulse.Api.csproj ProjectPulse.Api/packages.lock.json ProjectPulse.Api/
RUN dotnet restore ProjectPulse.Api/ProjectPulse.Api.csproj --locked-mode

COPY ProjectPulse.Api/ ProjectPulse.Api/
RUN dotnet publish ProjectPulse.Api/ProjectPulse.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0.28 AS runtime
WORKDIR /app

RUN mkdir -p /data && chown "$APP_UID" /data
COPY --from=build /app/publish/ .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ProjectPulse.Api.dll"]
