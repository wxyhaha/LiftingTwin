@echo off
:: 自动查找最新的 build 输出目录
for /f "delims=" %%d in ('dir /b /ad /o-n "%~dp0build*" 2^>nul') do (
    if exist "%~dp0%%d\Release\appLiftingTwinUI.exe" (
        set "LATEST_BUILD=%%d"
        goto :found
    )
)
echo Error: 找不到已构建的可执行文件
pause
exit /b 1
:found
set PATH=C:\Qt\6.5.3\msvc2019_64\bin;%PATH%
set QT_PLUGIN_PATH=C:\Qt\6.5.3\msvc2019_64\plugins
set QML2_IMPORT_PATH=C:\Qt\6.5.3\msvc2019_64\qml
echo Starting LiftingTwin UI...
"%~dp0%LATEST_BUILD%\Release\appLiftingTwinUI.exe"
echo Exit code: %ERRORLEVEL%
