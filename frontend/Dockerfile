# /frontend/Dockerfile

# --- Giai đoạn 1: Build ---
FROM node:20-alpine AS build
WORKDIR /app

# THAY ĐỔI Ở ĐÂY:
# Chỉ rõ là copy file package.json từ thư mục 'frontend'
COPY frontend/package*.json ./

# Bây giờ npm install sẽ tìm thấy /app/package.json
RUN npm install

# THAY ĐỔI Ở ĐÂY:
# Copy toàn bộ code của thư mục 'frontend' vào /app
COPY frontend/ .

RUN NODE_OPTIONS=--max-old-space-size=6144 npm run build

# --- Giai đoạn 2: Final Image ---
FROM node:20-alpine AS final
WORKDIR /app
ENV NODE_ENV=production

# Copy file build từ giai đoạn 1
# Thư mục .output sẽ nằm trong /app/.output
COPY --from=build /app/.output .

EXPOSE 3000
CMD ["node", "server/index.mjs"]