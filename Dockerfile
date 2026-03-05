FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app
EXPOSE 5010


ENV ASPNETCORE_URLS=http://+:5010

RUN apt-get update && \
    apt-get install wget unzip curl jq \
    perl perl-base perl-modules libclone-perl libdate-manip-perl libdatetime-format-strptime-perl libdatetime-perl libjson-perl libmath-bigint-perl libmath-round-perl libswitch-perl libtext-csv-perl liburi-perl -y

USER appuser

FROM node:24 AS vuebuild
WORKDIR /src
COPY ["vue", "vue"]
WORKDIR "/src/vue"
RUN npm ci
RUN npm run build 

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
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
