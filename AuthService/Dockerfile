# Etapa 1: Construir la aplicación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia el .csproj y restaura los paquetes
COPY *.csproj .
RUN dotnet restore

# Copia el resto del código y compila
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Crear la imagen final
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponemos el mismo puerto estándar 8080 (HTTP)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthService.dll"]