# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Restore dependencies (cached layer)
COPY src/CliniSys.Domain/CliniSys.Domain.csproj          src/CliniSys.Domain/
COPY src/CliniSys.Application/CliniSys.Application.csproj src/CliniSys.Application/
COPY src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj src/CliniSys.Infrastructure/
COPY src/CliniSys.Api/CliniSys.Api.csproj                 src/CliniSys.Api/
RUN dotnet restore src/CliniSys.Api/CliniSys.Api.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/CliniSys.Api/CliniSys.Api.csproj \
    -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Non-root user for security
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "CliniSys.Api.dll"]
