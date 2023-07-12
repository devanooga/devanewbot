FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base

WORKDIR /app
EXPOSE 5010


ENV ASPNETCORE_URLS=http://+:5010

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


FROM mcr.microsoft.com/dotnet/sdk:7.0 AS publish
WORKDIR /src
COPY . .
WORKDIR "/src"
RUN dotnet build "devanewbot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=vuebuild /src/wwwroot wwwroot
ENTRYPOINT ["dotnet", "devanewbot.dll"]
