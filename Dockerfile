# Jame-Jafari API — multi-stage publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/JameJafari.Core/JameJafari.Core.csproj JameJafari.Core/
COPY src/JameJafari.Infrastructure/JameJafari.Infrastructure.csproj JameJafari.Infrastructure/
COPY src/JameJafari.Api/JameJafari.Api.csproj JameJafari.Api/
RUN dotnet restore JameJafari.Api/JameJafari.Api.csproj

COPY src/JameJafari.Core/ JameJafari.Core/
COPY src/JameJafari.Infrastructure/ JameJafari.Infrastructure/
COPY src/JameJafari.Api/ JameJafari.Api/
RUN dotnet publish JameJafari.Api/JameJafari.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /app/uploads

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
VOLUME ["/app/uploads"]

ENTRYPOINT ["dotnet", "JameJafari.Api.dll"]
