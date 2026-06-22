# Radmin VPN Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Configure and document a distributed multi-group deployment of microservices over Radmin VPN, customized for Group 2 (Order service + shared gateway, rabbitmq, and frontend).

**Architecture:** Docker compose based deployment where Group 2 runs their own DB, gateway, RabbitMQ, frontend, and order service. Other groups' services are integrated by targeting their mapped Radmin VPN IPs and ports.

**Tech Stack:** Docker, Docker Compose, Nginx, .NET 10, Vue.js (Vite).

---

### Task 1: Update Frontend Dockerfile to accept build-time API Base URL

**Files:**
- Modify: `frontend/typescript-version/Dockerfile:1-14`

- [ ] **Step 1: Modify the Dockerfile to support VITE_API_BASE_URL argument**
  Change the build stage of `frontend/typescript-version/Dockerfile` to include the `ARG` and `ENV` parameters.
  
  ```dockerfile
  # Stage 1: Build stage
  FROM node:20-alpine AS build-stage
  WORKDIR /app
  ARG VITE_API_BASE_URL
  ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
  COPY . .
  RUN npm install
  RUN npm run build
  
  # Stage 2: Production stage using Nginx
  FROM nginx:stable-alpine AS production-stage
  COPY --from=build-stage /app/dist /usr/share/nginx/html
  COPY default.conf /etc/nginx/conf.d/default.conf
  EXPOSE 80
  CMD ["nginx", "-g", "daemon off;"]
  ```

- [ ] **Step 2: Commit**
  ```bash
  git add frontend/typescript-version/Dockerfile
  git commit -m "refactor(frontend): support dynamic VITE_API_BASE_URL at docker build time"
  ```

---

### Task 2: Create Environment Configuration Template for Group 2

**Files:**
- Create: `.env.group2`

- [ ] **Step 1: Create `.env.group2` at the root directory**
  Define variables for Radmin VPN IPs and local credentials.

  ```ini
  # ==============================================================================
  # CẤU HÌNH IP RADMIN VPN CÁC NHÓM
  # ==============================================================================
  RADMIN_IP_NHOM1=26.11.11.11
  RADMIN_IP_NHOM2=26.22.22.22
  RADMIN_IP_NHOM3=26.33.33.33

  # ==============================================================================
  # CẤU HÌNH HẠ TẦNG (LOCAL INFRASTRUCTURE CONFIG)
  # ==============================================================================
  MSSQL_SA_PASSWORD=SuperStrong@Password123
  MSSQL_PID=Developer

  RABBITMQ_DEFAULT_USER=guest
  RABBITMQ_DEFAULT_PASS=guest

  # ==============================================================================
  # BẢO MẬT & XÁC THỰC JWT
  # ==============================================================================
  JWT_SECRET=this-is-a-very-long-and-super-secure-secret-key-32-bytes-long!
  JWT_ISSUER=RetailSystemApi
  JWT_AUDIENCE=RetailSystemClient
  ```

- [ ] **Step 2: Commit**
  ```bash
  git add .env.group2
  git commit -m "feat(env): add environment configuration template for Group 2"
  ```

---

### Task 3: Create Docker Compose configuration for Group 2

**Files:**
- Create: `docker-compose.group2.yml`

- [ ] **Step 1: Create `docker-compose.group2.yml` at the root directory**
  Configure the services to exclude Group 1 and Group 3 microservices and wire HTTP/RabbitMQ connections using Radmin VPN IPs.

  ```yaml
  services:
    sqlserver:
      image: mcr.microsoft.com/mssql/server:2022-latest
      container_name: retail_sqlserver_nhom2
      environment:
        ACCEPT_EULA: "Y"
        MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD:-SuperStrong@Password123}
      ports:
        - "1433:1433"
      volumes:
        - sqlserver_data_nhom2:/var/opt/mssql
      networks:
        - retail_network_nhom2
      healthcheck:
        test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -Q \"SELECT 1\" || /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -Q \"SELECT 1\""]
        interval: 10s
        timeout: 5s
        retries: 10
        start_period: 30s
      restart: always

    rabbitmq:
      image: rabbitmq:3-management-alpine
      container_name: retail_rabbitmq_nhom2
      environment:
        RABBITMQ_DEFAULT_USER: ${RABBITMQ_DEFAULT_USER:-guest}
        RABBITMQ_DEFAULT_PASS: ${RABBITMQ_DEFAULT_PASS:-guest}
      ports:
        - "5672:5672"
        - "15672:15672"
      volumes:
        - rabbitmq_data_nhom2:/var/lib/rabbitmq
      networks:
        - retail_network_nhom2
      healthcheck:
        test: ["CMD", "rabbitmq-diagnostics", "check_running"]
        interval: 10s
        timeout: 5s
        retries: 10
      restart: always

    order-sales-service:
      build:
        context: ./src
        dockerfile: OrderSalesService/OrderSalesService.API/Dockerfile
      container_name: retail_order_sales_service_nhom2
      environment:
        ASPNETCORE_ENVIRONMENT: Development
        ASPNETCORE_URLS: http://+:80
        ConnectionStrings__DefaultConnection: Server=sqlserver;Database=OrderSalesDB;User Id=sa;Password=${MSSQL_SA_PASSWORD:-SuperStrong@Password123};TrustServerCertificate=True;
        RabbitMQ__HostName: rabbitmq
        RabbitMQ__UserName: ${RABBITMQ_DEFAULT_USER:-guest}
        RabbitMQ__Password: ${RABBITMQ_DEFAULT_PASS:-guest}
        InternalApis__ProductService: http://${RADMIN_IP_NHOM1}:5001
        JwtSettings__Secret: ${JWT_SECRET:-this-is-a-very-long-and-super-secure-secret-key-32-bytes-long!}
        JwtSettings__Issuer: ${JWT_ISSUER:-RetailSystemApi}
        JwtSettings__Audience: ${JWT_AUDIENCE:-RetailSystemClient}
      depends_on:
        sqlserver:
          condition: service_healthy
        rabbitmq:
          condition: service_healthy
      ports:
        - "5002:80"
      networks:
        - retail_network_nhom2
      restart: always

    api-gateway:
      build:
        context: ./src
        dockerfile: ApiGateway/ApiGateway/Dockerfile
      container_name: retail_api_gateway_nhom2
      environment:
        ASPNETCORE_ENVIRONMENT: Development
        ASPNETCORE_URLS: http://+:80
        DownstreamServices__ProductInventory: http://${RADMIN_IP_NHOM1}:5001
        DownstreamServices__OrderSales: http://order-sales-service:80
        DownstreamServices__UserReport: http://${RADMIN_IP_NHOM3}:5003
        Cors__AllowedOrigins__0: http://localhost:8080
        Cors__AllowedOrigins__1: http://127.0.0.1:8080
        Cors__AllowedOrigins__2: http://localhost:5173
        Cors__AllowedOrigins__3: http://127.0.0.1:5173
        Cors__AllowedOrigins__4: http://${RADMIN_IP_NHOM2}:8080
      depends_on:
        - order-sales-service
      ports:
        - "5000:80"
      networks:
        - retail_network_nhom2
      restart: always

    frontend:
      build:
        context: ./frontend/typescript-version
        dockerfile: Dockerfile
        args:
          - VITE_API_BASE_URL=http://${RADMIN_IP_NHOM2}:5000
      container_name: retail_frontend_nhom2
      depends_on:
        - api-gateway
      ports:
        - "8080:80"
      networks:
        - retail_network_nhom2
      restart: always

  networks:
    retail_network_nhom2:
      name: retail_network_nhom2
      driver: bridge

  volumes:
    sqlserver_data_nhom2:
    rabbitmq_data_nhom2:
  ```

- [ ] **Step 2: Commit**
  ```bash
  git add docker-compose.group2.yml
  git commit -m "feat(docker): add docker-compose config for Group 2 deployment over Radmin VPN"
  ```

---

### Task 4: Write comprehensive Multi-Group deployment guide

**Files:**
- Create: `docs/11-multi-group-deployment-guide.md`

- [ ] **Step 1: Create the markdown deployment guide**
  Write detailed instructions in Vietnamese explaining the topology, setup steps for Radmin VPN, configuration files for all 3 groups, startup sequence, and troubleshooting firewall/CORS issues.

- [ ] **Step 2: Commit**
  ```bash
  git add docs/11-multi-group-deployment-guide.md
  git commit -m "docs: add multi-group deployment guide over Radmin VPN"
  ```
