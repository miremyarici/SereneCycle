# Derleme aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Önce sadece proje dosyalarını kopyala: kaynak değişince restore
# katmanı önbellekten gelsin.
COPY SereneCycle.sln .
COPY src/SereneCycle.Domain/*.csproj src/SereneCycle.Domain/
COPY src/SereneCycle.Application/*.csproj src/SereneCycle.Application/
COPY src/SereneCycle.Infrastructure/*.csproj src/SereneCycle.Infrastructure/
COPY src/SereneCycle.Api/*.csproj src/SereneCycle.Api/
COPY tests/SereneCycle.Tests/*.csproj tests/SereneCycle.Tests/
RUN dotnet restore src/SereneCycle.Api/SereneCycle.Api.csproj

COPY . .
RUN dotnet publish src/SereneCycle.Api/SereneCycle.Api.csproj \
    -c Release -o /app/publish --no-restore

# Çalışma aşaması
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Root olmayan kullanıcı ile çalış.
RUN adduser --disabled-password --gecos "" --uid 1001 appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SereneCycle.Api.dll"]
