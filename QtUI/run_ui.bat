@echo off
chcp 65001 >nul
cd /d "%~dp0"
if "%1"=="" (
    echo 用法: run_ui.bat SystemMonitor
    echo       run_ui.bat TrolleyControl
    echo       run_ui.bat TrajectoryPrediction
    pause
    exit /b
)
set APP_DIR=%~dp0build5\Release
set QML2_IMPORT_PATH=%APP_DIR%\qml
echo [启动] %1
"%APP_DIR%\appLiftingTwinUI.exe" --ui %1
if errorlevel 1 pause
