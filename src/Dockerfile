FROM mcr.microsoft.com/dotnet/aspnet:5.0.4 AS base

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /build
COPY . .

RUN dotnet restore "TimonWebApp.Server/TimonWebApp.Server.fsproj"

FROM build AS publish
RUN dotnet publish "TimonWebApp.Server/TimonWebApp.Server.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN cp .MyCertificate.crt /usr/local/share/ca-certificates
RUN update-ca-certificates

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="http://*:8080"
EXPOSE 8080

# RUN addgroup --gid 998 --system appgroup \
#     && adduser --uid 1004 --system appuser --ingroup appgroup

# USER appuser

ENTRYPOINT ["dotnet", "TimonWebApp.Server.dll"]
