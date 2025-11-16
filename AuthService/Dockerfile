# Etapa 1: Construir la aplicación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia el .csproj y restaura los paquetes
# (Asume que el .proto está en Protos/clients.proto)
COPY *.csproj .
RUN dotnet restore

# Copia el resto del código y compila
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Crear la imagen final
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# IMPORTANTE: Dentro de Docker, usaremos HTTP. NGINX manejará HTTPS.
# El puerto 8080 es estándar para servicios .NET en contenedores.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClientsService.dll"]