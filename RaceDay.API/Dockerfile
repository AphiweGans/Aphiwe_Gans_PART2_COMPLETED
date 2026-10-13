# Dockerfile for RaceDay.API. Build context is the repository root (see docker-compose.yml)
# so the shared RaceDay.Contracts project is available.
# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RaceDay.Contracts/RaceDay.Contracts.csproj RaceDay.Contracts/
COPY RaceDay.API/RaceDay.API.csproj RaceDay.API/
RUN dotnet restore "RaceDay.API/RaceDay.API.csproj"

COPY RaceDay.Contracts/ RaceDay.Contracts/
COPY RaceDay.API/ RaceDay.API/
RUN dotnet publish "RaceDay.API/RaceDay.API.csproj" -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
