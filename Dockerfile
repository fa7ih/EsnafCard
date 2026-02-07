# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Tüm dosyaları kopyala
COPY . .

# SecureCardSystem.csproj dosyasını bul ve restore et
RUN dotnet restore "SecureCardSystem.csproj"

# Build et
RUN dotnet publish "SecureCardSystem.csproj" -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Railway için port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SecureCardSystem.dll"]