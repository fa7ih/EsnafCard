# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve restore et
COPY ["SecureCardSystem.csproj", "./"]
RUN dotnet restore "SecureCardSystem.csproj"

# Tüm dosyaları kopyala ve build et
COPY . .
RUN dotnet publish "SecureCardSystem.csproj" -c Release -o /app/publish

# Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Port ayarı
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SecureCardSystem.dll"]