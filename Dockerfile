FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ease_intro_api.csproj ./
RUN dotnet restore ease_intro_api.csproj
COPY . .
RUN dotnet publish ease_intro_api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS test
WORKDIR /src
COPY ease_intro_api.csproj ./
COPY Tests/Tests.csproj Tests/
RUN dotnet restore Tests/Tests.csproj
COPY . .
RUN dotnet build Tests/Tests.csproj -c Release --no-restore
ENTRYPOINT ["dotnet", "test", "Tests/Tests.csproj", "-c", "Release", "--no-build", "--no-restore"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Kestrel__Endpoints__Http__Url=http://0.0.0.0:8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ease_intro_api.dll"]
