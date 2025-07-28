# Usar imagen base de .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Railway usa puerto din�mico
ENV PORT=8080
EXPOSE $PORT

# Imagen para build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["StudentRegistration.Api/StudentRegistration.Api.csproj", "StudentRegistration.Api/"]
RUN dotnet restore "StudentRegistration.Api/StudentRegistration.Api.csproj"

# Copiar todo el c�digo fuente
COPY . .
WORKDIR "/src/StudentRegistration.Api"

# Build del proyecto
RUN dotnet build "StudentRegistration.Api.csproj" -c Release -o /app/build

# Publicar la aplicaci�n
FROM build AS publish
RUN dotnet publish "StudentRegistration.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Railway usa variable PORT
ENV ASPNETCORE_URLS=http://+:$PORT

ENTRYPOINT ["dotnet", "StudentRegistration.Api.dll"]