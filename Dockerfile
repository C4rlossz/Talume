FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/Talume.Web/Talume.Web.csproj src/Talume.Web/
RUN dotnet restore src/Talume.Web/Talume.Web.csproj
COPY src/ src/
COPY tests/Talume.MailTests/ tests/Talume.MailTests/
RUN dotnet run --project tests/Talume.MailTests -c Release
RUN dotnet publish src/Talume.Web/Talume.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER root
RUN apt-get update && apt-get install -y --no-install-recommends gosu && rm -rf /var/lib/apt/lists/*
COPY --chmod=755 docker-entrypoint.sh /usr/local/bin/talume-entrypoint
ENV ASPNETCORE_URLS=http://+:8080
ENV DataProtection__Path=/app/keys
EXPOSE 8080
ENTRYPOINT ["/usr/local/bin/talume-entrypoint"]
