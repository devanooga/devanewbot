FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
EXPOSE 5010


ENV ASPNETCORE_URLS=http://+:5010

RUN apt-get update && \
    apt-get install wget unzip curl jq \
    perl perl-base perl-modules libclone-perl libdate-manip-perl libdatetime-format-strptime-perl libdatetime-perl libjson-perl libmath-bigint-perl libmath-round-perl libswitch-perl libtext-csv-perl liburi-perl -y
# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM node:18-bullseye-slim AS vuebuild
WORKDIR /src
COPY ["vue", "vue"]
WORKDIR "/src/vue"
RUN npm ci
RUN export NODE_OPTIONS=--openssl-legacy-provider && \
    npm run build 

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY . .
WORKDIR "/src"
RUN dotnet build "devanewbot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=vuebuild /src/wwwroot wwwroot
RUN wget https://github.com/molo1134/qrmbot/archive/refs/heads/master.zip && \
    unzip master.zip && \
    mv qrmbot-master qrmbot
ENTRYPOINT ["dotnet", "devanewbot.dll"]
