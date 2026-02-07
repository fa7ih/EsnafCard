FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# EF Tools'u yükle
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Proje dosyalarını kopyala
COPY SecureCardSystem.csproj .
RUN dotnet restore SecureCardSystem.csproj

COPY . .
RUN dotnet publish SecureCardSystem.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE $PORT

# Migration çalıştır ve uygulamayı başlat
CMD dotnet SecureCardSystem.dll
