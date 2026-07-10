@echo off
set PATH=C:\Qt\6.5.3\msvc2019_64\bin;%PATH%
set QT_PLUGIN_PATH=C:\Qt\6.5.3\msvc2019_64\plugins
set QML2_IMPORT_PATH=C:\Qt\6.5.3\msvc2019_64\qml
echo Starting LiftingTwin UI...
c:\Users\Administrator\Documents\unity\LiftingTwin\QtUI\build3\Release\appLiftingTwinUI.exe
echo Exit code: %ERRORLEVEL%
