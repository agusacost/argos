# ── Build stage: compila la SPA con Vite ──────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json ./
RUN npm install
COPY . .
# VITE_API_BASE se deja en '/api' (default): el nginx de runtime proxya a rag-api.
RUN npm run build

# ── Runtime stage: nginx sirve los estáticos y proxya /api → rag-api ──────────
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
