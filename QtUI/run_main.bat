@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo =======================================================
echo   输变电工程吊装作业三维动态安全管控平台
echo   LiftingTwin QtUI Desktop Shell
echo =======================================================
echo.

REM 检测 Qt 运行环境
set APP_DIR=%~dp0build5\Release
if not exist "%APP_DIR%\appLiftingTwinUI.exe" (
    echo [错误] 找不到 %APP_DIR%\appLiftingTwinUI.exe
    echo   请先运行 setup_and_build.bat
    pause
    exit /b 1
)

REM 确保 QML 模块路径正确
set QML2_IMPORT_PATH=%APP_DIR%\qml

echo [启动] 主界面（+ Unity 三维场景）
echo.
echo   子窗口可在标题栏下方点击按钮打开：
echo     [系统监控] [小车控制] [轨迹预测]
echo.
"%APP_DIR%\appLiftingTwinUI.exe"
if errorlevel 1 (
    echo [警告] 程序异常退出 (code %errorlevel%)
    pause
)
