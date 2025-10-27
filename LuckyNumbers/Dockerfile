FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LuckyNumbers/LuckyNumbers.csproj", "LuckyNumbers/"]
COPY ["Directory.Build.props", "Directory.Packages.props", "nuget.config", ".editorconfig", "./"]

RUN dotnet restore "LuckyNumbers/LuckyNumbers.csproj"
COPY ./LuckyNumbers ./LuckyNumbers
WORKDIR "/src/LuckyNumbers"
RUN dotnet build "LuckyNumbers.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LuckyNumbers.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LuckyNumbers.dll"]
