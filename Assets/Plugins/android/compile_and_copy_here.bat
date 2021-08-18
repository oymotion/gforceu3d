
@echo off

@SET HOME=%~dp0
@echo Home: %HOME%


@echo.
@echo compiling lib ble
@echo.


cd %HOME%..\..\..\..\..\..\script\android\gForceSDK\
call gradlew assembleRelease

@echo.
@REM @echo Copying 'ble-release.aar' and 'AndroidManifest.xml'
@echo Copying 'ble-release.aar'
@echo.

copy /Y ble\build\outputs\aar\ble-release.aar %HOME%
@REM copy /Y ble\src\main\AndroidManifest.xml %HOME%

@echo.
@echo done
@echo.


@echo.
@echo compiling lib gforce
@echo.

cd %HOME%..\..\..\..\libgforce\android\gforce
call gradlew assembleRelease

@echo.
@echo Copying 'libgforce-release.aar'
@echo.

copy /Y libgforce\build\outputs\aar\libgforce-release.aar %HOME%

@echo.
@echo done
@echo.


pause
