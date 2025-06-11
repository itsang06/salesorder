@echo off
setlocal enabledelayedexpansion

:: Kiểm tra Docker đã chạy chưa
docker info >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Docker chưa được khởi động! Vui lòng mở Docker Desktop và chạy lại script.
    pause
    exit /b 1
)

:: Định nghĩa thông tin AWS ECR
set AWS_ECR=284704742482.dkr.ecr.ap-southeast-1.amazonaws.com
set WORKING_DIR=ODSaleOrder.API
set JSON_FILE=%WORKING_DIR%\aws-ecs-tools-defaults.json

:: Kiểm tra file JSON có tồn tại không
if not exist "%JSON_FILE%" (
    echo Không tìm thấy file %JSON_FILE%!
    exit /b 1
)

:: Lấy giá trị image-tag từ file JSON bằng PowerShell
for /f "delims=" %%i in ('powershell -Command "(Get-Content %JSON_FILE% -Raw | ConvertFrom-Json | Select-Object -ExpandProperty image-tag)"') do set IMAGE_TAG=%%i

:: Kiểm tra nếu IMAGE_TAG rỗng
if "%IMAGE_TAG%"=="" (
    echo Không tìm thấy image-tag trong file %JSON_FILE%!
    exit /b 1
)

:: Tách tên image và version (odsaleorderapi:1.0.44.6)
for /f "tokens=1,2 delims=:" %%a in ("%IMAGE_TAG%") do (
    set IMAGE_NAME=%%a
    set VERSION=%%b
)

:: Tách version thành các phần (1.0.44.6 -> 1 0 44 6)
for /f "tokens=1,2,3,4 delims=." %%a in ("%VERSION%") do (
    set V1=%%a
    set V2=%%b
    set V3=%%c
    set V4=%%d
)

:: Tăng giá trị cuối cùng
set /a V4=V4+1

:: Tạo phiên bản mới
set NEW_VERSION=%V1%.%V2%.%V3%.%V4%
set NEW_IMAGE_TAG=%AWS_ECR%/%IMAGE_NAME%:%NEW_VERSION%


:: Kiểm tra nếu cập nhật JSON thành công
if %ERRORLEVEL% NEQ 0 (
    echo Có lỗi xảy ra khi cập nhật file JSON.
    pause
    exit /b 1
)

:: Build Docker image với phiên bản mới, trỏ đúng vào Dockerfile
echo Đang build Docker image với tag: %NEW_IMAGE_TAG%
docker build -t %NEW_IMAGE_TAG% -f %WORKING_DIR%\Dockerfile .

echo Build hoàn tất!

:: Đăng nhập AWS ECR (chỉ chạy nếu bạn muốn push)
aws ecr get-login-password --region ap-southeast-1 | docker login --username AWS --password-stdin %AWS_ECR%

:: Push Docker image lên AWS ECR
docker push %NEW_IMAGE_TAG%

echo Push image lên AWS ECR hoàn tất!

pause
exit /b 0
