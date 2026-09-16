FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/Talume.Web/Talume.Web.csproj src/Talume.Web/
RUN dotnet restore src/Talume.Web/Talume.Web.csproj
COPY src/ src/
RUN dotnet publish src/Talume.Web/Talume.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER root
RUN mkdir -p /app/keys && chown -R app:app /app/keys
USER app
ENV ASPNETCORE_URLS=http://+:8080
ENV DataProtection__Path=/app/keys
EXPOSE 8080
ENTRYPOINT ["dotnet", "Talume.Web.dll"]
