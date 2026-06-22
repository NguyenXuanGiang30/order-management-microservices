@echo off
echo =====================================================================
echo KHOI CHAY MICROSERVICES CHO NHOM 2 (ORDER VA SHARED INFRA) VIA RADMIN
echo =====================================================================
echo Dang doc cau hinh tu teps .env.radmin...
docker compose --env-file .env.radmin -f docker-compose.group2.yml up -d --build
echo.
echo Da hoan thanh! Nhan phim bat ky de thoat.
pause > nul
