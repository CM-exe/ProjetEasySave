# ---------- Étape 1 : Build avec le SDK ----------
FROM mcr.microsoft.com/dotnet/nightly/sdk:10.0 AS build
WORKDIR /src

# Copier le projet et restaurer les dépendances
COPY *.csproj ./
RUN dotnet restore

# Copier tout le code et compiler
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ---------- Étape 2 : Image runtime ----------
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0 AS runtime
WORKDIR /app

# Répertoire des logs
RUN mkdir -p /app/logs

# Copier les fichiers compilés depuis l'étape build
COPY --from=build /app/publish .

# Port exposé
EXPOSE 8080

# Point d’entrée de l’application
ENTRYPOINT ["dotnet", "EasySaveCentralisedLogs.dll"]