# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos csproj (para cachear restore)
COPY src/BankTransfer.Api/BankTransfer.Api.csproj src/BankTransfer.Api/
COPY src/BankTransfer.Application/BankTransfer.Application.csproj src/BankTransfer.Application/
COPY src/BankTransfer.Domain/BankTransfer.Domain.csproj src/BankTransfer.Domain/
COPY src/BankTransfer.Infrastructure/BankTransfer.Infrastructure.csproj src/BankTransfer.Infrastructure/

# Restore SOLO de la API (trae refs a los otros proyectos)
RUN dotnet restore src/BankTransfer.Api/BankTransfer.Api.csproj

# Copiamos el resto del código
COPY . .

# Publicamos
RUN dotnet publish src/BankTransfer.Api/BankTransfer.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BankTransfer.Api.dll"]
