@echo off
set PATH=C:\Qt\6.5.3\msvc2019_64\bin;%PATH%
set QT_PLUGIN_PATH=C:\Qt\6.5.3\msvc2019_64\plugins
cd /d "%~dp0build\Release"
appLiftingTwinUI.exe
