FROM node:20-alpine AS build
WORKDIR /app
COPY src/BigBrain.Web/package*.json ./
RUN npm ci
COPY src/BigBrain.Web/ ./
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime
COPY src/BigBrain.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
