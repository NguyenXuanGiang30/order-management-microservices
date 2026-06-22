@echo off
echo =====================================================================
echo KHOI CHAY MICROSERVICES CHOD NHOM 3 (USER & REPORT) VIA RADMIN
echo =====================================================================
echo Dang doc cau hinh tu teps .env.radmin...
docker compose --env-file .env.radmin -f docker-compose.group3.yml up -d --build
echo.
echo Da hoan thanh! Nhan phim bat ky de thoat.
pause > nul
