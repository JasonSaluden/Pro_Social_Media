# Image tout-en-un : ASP.NET API + MySQL + MongoDB
# Usage: docker build -t prosocial .

FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive

# Installation des dépendances de base
RUN apt-get update && apt-get install -y \
    wget \
    curl \
    gnupg \
    supervisor \
    && rm -rf /var/lib/apt/lists/*

# Installation de .NET 9 Runtime
RUN wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y aspnetcore-runtime-9.0 dotnet-sdk-9.0 \
    && rm -rf /var/lib/apt/lists/*

# Installation de MySQL 8.0
RUN apt-get update && apt-get install -y mysql-server \
    && rm -rf /var/lib/apt/lists/*

# Installation de MongoDB 7.0
RUN curl -fsSL https://www.mongodb.org/static/pgp/server-7.0.asc | gpg --dearmor -o /usr/share/keyrings/mongodb-server-7.0.gpg \
    && echo "deb [ arch=amd64,arm64 signed-by=/usr/share/keyrings/mongodb-server-7.0.gpg ] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/7.0 multiverse" | tee /etc/apt/sources.list.d/mongodb-org-7.0.list \
    && apt-get update \
    && apt-get install -y mongodb-org \
    && rm -rf /var/lib/apt/lists/*

# Création des répertoires nécessaires
RUN mkdir -p /var/run/mysqld /var/lib/mongodb /data/db /app/wwwroot/uploads \
    && chown mysql:mysql /var/run/mysqld \
    && chown mongodb:mongodb /var/lib/mongodb /data/db

# Configuration MySQL
RUN echo "[mysqld]\nbind-address=127.0.0.1\nskip-host-cache\nskip-name-resolve" >> /etc/mysql/mysql.conf.d/mysqld.cnf

# Copie du code source et build
WORKDIR /src
COPY . .
RUN dotnet restore && dotnet publish -c Release -o /app

# Script d'initialisation MySQL
RUN echo '#!/bin/bash\n\
mysqld --initialize-insecure --user=mysql\n\
mysqld --user=mysql &\n\
sleep 10\n\
mysql -u root <<EOF\n\
CREATE DATABASE IF NOT EXISTS pro_social_db;\n\
CREATE USER IF NOT EXISTS "prosocial"@"localhost" IDENTIFIED BY "prosocial123";\n\
GRANT ALL PRIVILEGES ON pro_social_db.* TO "prosocial"@"localhost";\n\
FLUSH PRIVILEGES;\n\
EOF\n\
' > /init-mysql.sh && chmod +x /init-mysql.sh

# Script de démarrage principal
RUN echo '#!/bin/bash\n\
\n\
# Initialiser MySQL si premier démarrage\n\
if [ ! -d "/var/lib/mysql/mysql" ]; then\n\
    echo "Initializing MySQL..."\n\
    mysqld --initialize-insecure --user=mysql\n\
    echo "Starting MySQL temporarily for setup..."\n\
    mysqld --user=mysql &\n\
    sleep 5\n\
    echo "Creating database and user..."\n\
    mysql -u root <<EOF\n\
CREATE DATABASE IF NOT EXISTS pro_social_db;\n\
CREATE USER IF NOT EXISTS "prosocial"@"localhost" IDENTIFIED BY "prosocial123";\n\
CREATE USER IF NOT EXISTS "prosocial"@"127.0.0.1" IDENTIFIED BY "prosocial123";\n\
CREATE USER IF NOT EXISTS "prosocial"@"%" IDENTIFIED BY "prosocial123";\n\
GRANT ALL PRIVILEGES ON pro_social_db.* TO "prosocial"@"localhost";\n\
GRANT ALL PRIVILEGES ON pro_social_db.* TO "prosocial"@"127.0.0.1";\n\
GRANT ALL PRIVILEGES ON pro_social_db.* TO "prosocial"@"%";\n\
FLUSH PRIVILEGES;\n\
EOF\n\
    echo "Stopping temporary MySQL..."\n\
    mysqladmin -u root shutdown\n\
    sleep 2\n\
fi\n\
\n\
# Démarrer supervisord\n\
exec /usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf\n\
' > /start.sh && chmod +x /start.sh

# Configuration Supervisord
RUN echo '[supervisord]\n\
nodaemon=true\n\
user=root\n\
logfile=/var/log/supervisor/supervisord.log\n\
pidfile=/var/run/supervisord.pid\n\
\n\
[program:mysql]\n\
command=/usr/sbin/mysqld --user=mysql\n\
autostart=true\n\
autorestart=true\n\
stderr_logfile=/var/log/mysql/error.log\n\
stdout_logfile=/var/log/mysql/mysql.log\n\
priority=1\n\
\n\
[program:mongodb]\n\
command=/usr/bin/mongod --bind_ip 127.0.0.1 --dbpath /data/db\n\
autostart=true\n\
autorestart=true\n\
stderr_logfile=/var/log/mongodb/error.log\n\
stdout_logfile=/var/log/mongodb/mongod.log\n\
priority=2\n\
\n\
[program:api]\n\
command=dotnet /app/ProSocialApi.dll\n\
directory=/app\n\
autostart=true\n\
autorestart=true\n\
stderr_logfile=/var/log/api/error.log\n\
stdout_logfile=/var/log/api/api.log\n\
priority=3\n\
startsecs=15\n\
environment=ASPNETCORE_ENVIRONMENT="Docker",ASPNETCORE_URLS="http://+:8080"\n\
' > /etc/supervisor/conf.d/supervisord.conf

# Création des répertoires de logs
RUN mkdir -p /var/log/supervisor /var/log/mysql /var/log/mongodb /var/log/api

WORKDIR /app
EXPOSE 8080

# Variables d'environnement pour l'API
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

CMD ["/start.sh"]
