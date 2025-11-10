FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

# Copy configuration files explicitly
COPY ocelot.json .
COPY appsettings.Development.json .

EXPOSE 5227

# Run app
ENTRYPOINT ["dotnet", "GW.dll"]
