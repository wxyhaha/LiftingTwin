@echo off
set PATH=C:\Qt\6.5.3\msvc2019_64\bin;%PATH%
set QT_PLUGIN_PATH=C:\Qt\6.5.3\msvc2019_64\plugins
set QML2_IMPORT_PATH=C:\Qt\6.5.3\msvc2019_64\qml
set QT_LOGGING_RULES=*.debug=true
echo Environment:
echo PATH=%PATH%
echo QT_PLUGIN_PATH=%QT_PLUGIN_PATH%
echo QML2_IMPORT_PATH=%QML2_IMPORT_PATH%
echo.
echo Running qmlscene...
C:\Qt\6.5.3\msvc2019_64\bin\qmlscene.exe c:\Users\Administrator\Documents\unity\LiftingTwin\QtUI\simple.qml
echo Exit code: %ERRORLEVEL%
