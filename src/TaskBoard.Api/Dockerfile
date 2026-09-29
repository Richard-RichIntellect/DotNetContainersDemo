# See https://aka.ms/customizecontainer to learn how to customize your
# debug container and how Visual Studio uses this file to build images for
# faster debugging.
#
# This is the standard shape Visual Studio's own "Add > Docker Support"
# (or Container Orchestrator Support) wizard generates for an ASP.NET Core
# web project — not hand-invented for this demo. Build context is the
# repository root (see docker-compose.yml), which is why the COPY paths
# below start with src/TaskBoard.Api/ rather than being relative to this
# file's own folder.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/TaskBoard.Api/TaskBoard.Api.csproj", "src/TaskBoard.Api/"]
RUN dotnet restore "src/TaskBoard.Api/TaskBoard.Api.csproj"
COPY . .
WORKDIR "/src/src/TaskBoard.Api"
RUN dotnet build "TaskBoard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TaskBoard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskBoard.Api.dll"]
