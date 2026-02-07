FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# CSPROJ dosyanızın adını buraya yazın
COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# DLL dosya adınızı buraya yazın (CSPROJ adınızla aynı olmalı)
ENTRYPOINT ["dotnet", "YourProjectName.dll"]