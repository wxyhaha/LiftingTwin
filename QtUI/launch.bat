@echo off
set PATH=C:\Qt\6.5.3\msvc2019_64\bin;%~dp0build3\Release;%PATH%
set QT_PLUGIN_PATH=C:\Qt\6.5.3\msvc2019_64\plugins
cd /d "%~dp0build3\Release"
echo Starting LiftingTwin UI...
echo PATH=%PATH%
appLiftingTwinUI.exe
pause
