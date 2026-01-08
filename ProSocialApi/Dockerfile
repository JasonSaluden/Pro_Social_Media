# Dockerfile pour ProSocialApi
# Build multi-stage pour optimiser la taille de l'image

# === Stage 1 : Build ===
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copier le fichier projet et restaurer les dépendances
COPY ["ProSocialApi.csproj", "."]
RUN dotnet restore

# Copier le reste du code source
COPY . .

# Build en mode Release
RUN dotnet build -c Release -o /app/build

# Publier l'application
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# === Stage 2 : Runtime ===
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Créer le dossier pour les uploads
RUN mkdir -p /app/wwwroot/uploads/avatars

# Copier l'application publiée
COPY --from=build /app/publish .

# Exposer le port
EXPOSE 8080

# Variables d'environnement par défaut
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Point d'entrée
ENTRYPOINT ["dotnet", "ProSocialApi.dll"]
