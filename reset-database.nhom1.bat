@echo off
chcp 65001 > nul
echo =====================================================================
echo    RESET VA SEED TU DONG CO SO DU LIEU CHO NHOM 1
echo =====================================================================
echo.
echo Dang xoa sach kho luu tru cu (Docker Volumes cua SQL Server)...
docker compose -f docker-compose.group1.yml --env-file .env.radmin down -v

echo.
echo Dang khoi dong lai cac container va chay tu dong migration/seed...
docker compose -f docker-compose.group1.yml --env-file .env.radmin up -d --build

echo.
echo Da hoan thanh! Co so du lieu da duoc lam sach va nap du lieu moi.
echo.
pause
