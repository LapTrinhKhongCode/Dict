# --- Giai đoạn 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file dự án & restore
COPY Dict/Dict.csproj ./Dict/
COPY Import/Import.csproj ./Import/
COPY Dict/Dict.sln ./Dict/
RUN dotnet restore Dict/Dict.sln

# Sao chép toàn bộ mã nguồn
COPY . .

# Publish dự án chính
RUN dotnet publish Dict/Dict.sln -c Release -o /app/publish

# --- Giai đoạn 2: Final Image ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Chỉ expose cổng, không set ENV ở đây
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Dict.dll"]
