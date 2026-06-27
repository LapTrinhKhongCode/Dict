# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Dict/Dict.csproj ./Dict/
RUN dotnet restore ./Dict/Dict.csproj -r linux-x64

COPY . .
RUN dotnet publish ./Dict/Dict.csproj -c Release -o /app/publish -r linux-x64 --self-contained false && \
    find /root/.nuget/packages/ -type f -name "libhf_tokenizers.so" -path "*/linux-x64/*" -exec cp {} /app/publish/ \;
# --- Stage 2: Final Image ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

USER root
RUN apt-get update && \
    apt-get install -y ghostscript libc6 libstdc++6 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
RUN mkdir -p /app/cache && chown app:app /app /app/cache
COPY --chown=app:app --from=build /app/publish .

USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "Dict.dll"]
