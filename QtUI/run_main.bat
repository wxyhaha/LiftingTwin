@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo =======================================================
echo   输变电工程吊装作业三维动态安全管控平台
echo   LiftingTwin QtUI Desktop Shell
echo =======================================================
echo.

set APP_DIR=%~dp0build5\Release

if not exist "%APP_DIR%\appLiftingTwinUI.exe" (
    echo [错误] 找不到编译产物，请先运行 setup_and_build.bat
    pause
    exit /b 1
)

REM 确保 qt.conf 存在（帮助 QML 引擎找到 FluentUI 等模块）
if not exist "%APP_DIR%\qt.conf" (
    echo [Paths]> "%APP_DIR%\qt.conf"
    echo Qml2Imports=./qml>> "%APP_DIR%\qt.conf"
)

echo [启动] 主界面
echo   在标题栏左下角点击 [系统监控] [小车控制] [轨迹预测] 打开子窗口
echo.
start "" /D "%APP_DIR%" "%APP_DIR%\appLiftingTwinUI.exe"
