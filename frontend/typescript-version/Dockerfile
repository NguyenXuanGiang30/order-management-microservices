# Stage 1: Build stage
FROM node:20-alpine AS build-stage
WORKDIR /app
COPY . .
RUN npm install
RUN npm run build

# Stage 2: Production stage using Nginx
FROM nginx:stable-alpine AS production-stage
COPY --from=build-stage /app/dist /usr/share/nginx/html
COPY default.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
