@echo off
REM ==========================================================================
REM LiftingTwin Qt 桌面外壳 — 一键构建与启动脚本
REM 用法: setup_and_build.bat [build_dir]
REM 默认 build 目录: build5
REM ==========================================================================
setlocal enabledelayedexpansion

set BUILD_DIR=%1
if "%BUILD_DIR%"=="" set BUILD_DIR=build5

set QT_DIR=C:\Qt\6.5.3\msvc2019_64
set CMAKE=%UserProfile%\AppData\Roaming\Python\Python313\site-packages\cmake\data\bin\cmake.exe
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

if not exist "%CMAKE%" set CMAKE=cmake

echo [1/5] 初始化 FluentUI...
cd /d "%~dp0third_party"
bash setup_fluentui.sh
if errorlevel 1 (
    echo 错误: FluentUI 初始化失败，请检查网络连接
    exit /b 1
)

echo [2/5] CMake 配置...
cd /d "%~dp0"
"%CMAKE%" -B %BUILD_DIR% -G "Visual Studio 17 2022" -A x64 -DCMAKE_PREFIX_PATH="%QT_DIR%" 2>&1
if errorlevel 1 (
    echo 错误: CMake 配置失败
    exit /b 1
)

echo [3/5] 编译中...
"%CMAKE%" --build %BUILD_DIR% --config Release 2>&1
if errorlevel 1 (
    echo 错误: 编译失败
    exit /b 1
)

REM 复制 LiftingTwin QML 模块到输出目录（cmake qt_add_qml_module 只编译到资源，不自动复制模块文件）
if exist "%CD%\%BUILD_DIR%\LiftingTwin" (
    xcopy /E /Y "%CD%\%BUILD_DIR%\LiftingTwin" "%CD%\%BUILD_DIR%\Release\LiftingTwin\" >nul 2>&1
    echo   LiftingTwin QML 模块已复制
)

echo [4/5] 部署 Qt DLL...
set PATH=%QT_DIR%\bin;%PATH%
windeployqt --qmldir "%~dp0qml" "%CD%\%BUILD_DIR%\Release\appLiftingTwinUI.exe" 2>&1
if errorlevel 1 (
    echo 警告: windeployqt 失败，应用可能缺少 DLL
)

REM 同步 fluentuiplugin.dll 到 app 本地 qml 目录（避免加载旧缓存）
if exist "%QT_DIR%\qml\FluentUI\Release\fluentuiplugin.dll" (
    copy /Y "%QT_DIR%\qml\FluentUI\Release\fluentuiplugin.dll" "%CD%\%BUILD_DIR%\Release\qml\FluentUI\fluentuiplugin.dll" > nul 2>&1
)

REM 生成 qt.conf（双击运行时 QML 引擎通过它找到 FluentUI 和 LiftingTwin 模块）
echo [Paths]> "%CD%\%BUILD_DIR%\Release\qt.conf"
echo Qml2Imports=.>> "%CD%\%BUILD_DIR%\Release\qt.conf"
echo Qml2Imports=./qml>> "%CD%\%BUILD_DIR%\Release\qt.conf"

echo [5/5] 完成！
echo.
echo 启动界面：
echo   %CD%\%BUILD_DIR%\Release\appLiftingTwinUI.exe               (主界面 + Unity)
echo   %CD%\%BUILD_DIR%\Release\appLiftingTwinUI.exe --ui SystemMonitor
echo   %CD%\%BUILD_DIR%\Release\appLiftingTwinUI.exe --ui TrolleyControl
echo   %CD%\%BUILD_DIR%\Release\appLiftingTwinUI.exe --ui TrajectoryPrediction
