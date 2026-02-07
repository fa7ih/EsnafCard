# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Tüm dosyaları kopyala
COPY . .

# SecureCardSystem klasörüne git ve restore et
WORKDIR /src/SecureCardSystem
RUN dotnet restore "SecureCardSystem.csproj"

# Build et
RUN dotnet build "SecureCardSystem.csproj" -c Release -o /app/build

# Publish et
RUN dotnet publish "SecureCardSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Publish edilen dosyaları kopyala
COPY --from=build /app/publish .

# Railway için environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "SecureCardSystem.dll"]