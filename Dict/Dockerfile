# --- Giai đoạn 1: Build (Giữ nguyên) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file dự án & restore
COPY Dict/Dict.csproj ./Dict/
COPY Import/Import.csproj ./Import/
COPY Dict.Tests/Dict.Tests.csproj ./Dict.Tests/
COPY Dict/Dict.sln ./Dict/
RUN dotnet restore Dict/Dict.sln

# Sao chép toàn bộ mã nguồn
COPY . .

# Publish dự án chính
RUN dotnet publish Dict/Dict.sln -c Release -o /app/publish

# --- Giai đoạn 2: Final Image (ĐÃ SỬA) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# --- [THÊM VÀO] Cài đặt Ghostscript ---
# 1. Chuyển sang user 'root' để có quyền cài đặt
USER root
# 2. Chạy lệnh cài đặt Ghostscript (cho Linux/Debian)
RUN apt-get update && \
    apt-get install -y ghostscript && \
    rm -rf /var/lib/apt/lists/*
# 3. Chuyển về user 'app' (user mặc định, an toàn hơn)
USER app
# --- [KẾT THÚC THÊM VÀO] ---

WORKDIR /app
COPY --from=build /app/publish .

# Chỉ expose cổng, không set ENV ở đây
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Dict.dll"]
