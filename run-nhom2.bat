@echo off
echo =====================================================================
echo KHOI CHAY MICROSERVICES CHOD NHOM 2 (ORDER & SHARED INFRA) VIA RADMIN
echo =====================================================================
echo Dang doc cau hinh tu teps .env.radmin...
docker compose --env-file .env.radmin -f docker-compose.group2.yml up -d --build
echo.
echo Da hoan thanh! Nhan phim bat ky de thoat.
pause > nul
