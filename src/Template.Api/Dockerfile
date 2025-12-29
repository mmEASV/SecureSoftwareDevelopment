FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Template.Api/Template.Api.csproj", "Template.Api/"]
COPY ["Template.Shared/Template.Shared.csproj", "Template.Shared/"]
COPY ["Libraries/Community.Blazor.MapLibre/Community.Blazor.MapLibre/Community.Blazor.MapLibre.csproj", "Libraries/Community.Blazor.MapLibre/Community.Blazor.MapLibre/"]
COPY ["ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
RUN dotnet restore "Template.Api/Template.Api.csproj"
COPY . .
WORKDIR "/src/Template.Api"
RUN dotnet build "./Template.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Template.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Template.Api.dll"]
