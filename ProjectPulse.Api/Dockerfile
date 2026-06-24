# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ProjectPulse.Api.csproj
RUN dotnet publish ProjectPulse.Api.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# ✅ IMPORTANTE: cadena de conexión con el NOMBRE "Default"
ENV ConnectionStrings__Default=Data Source=projectpulse.db

# JWT settings (supply Jwt__Key at runtime)
ENV Jwt__Issuer=ProjectPulse
ENV Jwt__Audience=ProjectPulse
ENV Jwt__AccessTokenMinutes=30
ENV Jwt__RefreshTokenDays=7

ENTRYPOINT ["dotnet", "ProjectPulse.Api.dll"]
