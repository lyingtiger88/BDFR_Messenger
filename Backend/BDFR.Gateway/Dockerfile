FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Backend/ ./Backend/
RUN dotnet restore Backend/BDFR.Gateway/BDFR.Gateway.csproj
RUN dotnet publish Backend/BDFR.Gateway/BDFR.Gateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BDFR.Gateway.dll"]
