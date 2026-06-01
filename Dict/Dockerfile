  GNU nano 7.2                                                               Dict/Dockerfile
# --- Giai đoạn 1: Build (Chỉ tập trung vào dự án chính) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# CHỈ sao chép file dự án chính & restore trực tiếp nó
COPY Dict/Dict.csproj ./Dict/

RUN dotnet add ./Dict/Dict.csproj package Tokenizers.DotNet.runtime.linux-arm64

RUN dotnet restore ./Dict/Dict.csproj

# Sao chép toàn bộ mã nguồn hiện tại qua
COPY . .

# THAY ĐỔI: Publish trực tiếp file .csproj thay vì file .sln
RUN dotnet publish ./Dict/Dict.csproj -c Release -o /app/publish -r linux-arm64 --self-contained false
RUN find /root/.nuget/packages/ -type f -name "libhf_tokenizers.so" -exec cp {} /app/publish/ \;
# --- Giai đoạn 2: Final Image (Giữ nguyên cấu hình Ghostscript xịn của bạn) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# 1. Chuyển sang user 'root' để có quyền cài đặt
USER root
# 2. Chạy lệnh cài đặt Ghostscript (cho Linux/Debian)
RUN apt-get update && \
    apt-get install -y ghostscript libc6 libstdc++6 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
RUN chown app:app /app
# 2. 🔥 THAY ĐỔI QUYẾT ĐỊNH: Bàn giao toàn bộ quyền sở hữu file cho user 'app' ngay lúc copy
COPY --chown=app:app --from=build /app/publish .

# 3. Sau khi phân quyền thư mục xong xuôi, mới hạ quyền xuống 'app' để chạy ứng dụng an toàn
USER app

# Chỉ expose cổng HTTP 8080 (vì HTTPS đã có tầng ngoài lo)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Dict.dll"]
