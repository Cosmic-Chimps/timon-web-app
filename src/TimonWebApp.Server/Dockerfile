FROM mcr.microsoft.com/dotnet/aspnet:5.0.4 AS base
WORKDIR /app
COPY . .

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /build
COPY . .

RUN dotnet restore "TimonWebApp.Server.fsproj"

FROM build AS publish
RUN dotnet publish "TimonWebApp.Server.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN cp .MyCertificate.crt /usr/local/share/ca-certificates
RUN update-ca-certificates

RUN chmod 777 .MyCertificate.pfx

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="http://*:8088"
EXPOSE 8088

RUN addgroup --gid 998 --system appgroup \
    && adduser --uid 1004 --system appuser --ingroup appgroup

USER appuser

ENTRYPOINT ["dotnet", "TimonWebApp.Server.dll"]
