FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build

RUN apt-get update \
    && apt-get install -y --no-install-recommends libicu-dev ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY Backend/ ./Backend/
RUN dotnet restore Backend/BDFR.Gateway/BDFR.Gateway.csproj
RUN dotnet publish Backend/BDFR.Gateway/BDFR.Gateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble

RUN apt-get update \
    && apt-get install -y --no-install-recommends libicu-dev ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 8080
ENTRYPOINT ["dotnet", "BDFR.Gateway.dll"]
