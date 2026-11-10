# ── Build stage: export estático de Next.js (out/) ────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
# NEXT_PUBLIC_API_BASE se deja en '/api' (default): el nginx de runtime proxya a rag-api.
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

# ── Runtime stage: nginx sirve los estáticos y proxya /api → rag-api ──────────
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/out /usr/share/nginx/html
EXPOSE 80
