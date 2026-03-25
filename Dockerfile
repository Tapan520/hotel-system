# ── Build Stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy backend source (Dockerfile sits at repo root, backend/ is the subfolder)
COPY backend/ ./

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Railway injects PORT at runtime; Program.cs reads it via:
#   var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
#   app.Run($"http://0.0.0.0:{port}");
EXPOSE 8080

ENTRYPOINT ["dotnet", "HotelChannelManager.dll"]
