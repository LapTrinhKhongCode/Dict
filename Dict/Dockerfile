# --- Giai đoạn 1: Build (Chỉ tập trung vào dự án chính) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH=amd64
WORKDIR /src

# CHỈ sao chép file dự án chính & restore trực tiếp nó
COPY Dict/Dict.csproj ./Dict/

RUN if [ "$TARGETARCH" = "amd64" ]; then export DOTNET_RID=linux-x64; \
    elif [ "$TARGETARCH" = "arm64" ]; then export DOTNET_RID=linux-arm64; \
    else echo "Unsupported TARGETARCH: $TARGETARCH" && exit 1; fi && \
    dotnet restore ./Dict/Dict.csproj -r $DOTNET_RID

# Sao chép toàn bộ mã nguồn hiện tại qua
COPY . .

# THAY ĐỔI: Publish trực tiếp file .csproj thay vì file .sln
RUN if [ "$TARGETARCH" = "amd64" ]; then export DOTNET_RID=linux-x64; \
    elif [ "$TARGETARCH" = "arm64" ]; then export DOTNET_RID=linux-arm64; \
    else echo "Unsupported TARGETARCH: $TARGETARCH" && exit 1; fi && \
    dotnet publish ./Dict/Dict.csproj -c Release -o /app/publish -r $DOTNET_RID --self-contained false && \
    find /root/.nuget/packages/ -type f -name "libhf_tokenizers.so" -path "*/$DOTNET_RID/*" -exec cp {} /app/publish/ \;
# --- Giai đoạn 2: Final Image (Giữ nguyên cấu hình Ghostscript xịn của bạn) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# 1. Chuyển sang user 'root' để có quyền cài đặt
USER root
# 2. Chạy lệnh cài đặt Ghostscript (cho Linux/Debian)
RUN apt-get update && \
    apt-get install -y ghostscript libc6 libstdc++6 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
RUN mkdir -p /app/cache && chown app:app /app /app/cache
# 2. 🔥 THAY ĐỔI QUYẾT ĐỊNH: Bàn giao toàn bộ quyền sở hữu file cho user 'app' ngay lúc copy
COPY --chown=app:app --from=build /app/publish .

# 3. Sau khi phân quyền thư mục xong xuôi, mới hạ quyền xuống 'app' để chạy ứng dụng an toàn
USER app

# Chỉ expose cổng HTTP 8080 (vì HTTPS đã có tầng ngoài lo)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Dict.dll"]
