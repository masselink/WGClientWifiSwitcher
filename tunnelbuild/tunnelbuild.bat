@echo off
title MasselGUARD -- Tunnel DLL Builder
setlocal enabledelayedexpansion
echo.
echo  ====================================
echo       MasselGUARD -- Tunnel DLL
echo  ====================================
echo   Builds tunnel.dll from source and
echo   downloads wireguard-NT wireguard.dll
echo  ====================================
echo.
echo  Requires: Go 1.21+  https://go.dev/dl/
echo            gcc/MinGW  https://www.mingw-w64.org/
echo            git        https://git-scm.com/
echo.
echo  DLLs will be placed in:
echo    ..\wireguard-deps\tunnel.dll
echo    ..\wireguard-deps\wireguard.dll
echo.
echo  After this, run ..\BUILD.bat to compile MasselGUARD.
echo.

rem Parent folder of this script (project root)
set ROOT=%~dp0..
set DEPS=%ROOT%\wireguard-deps
set TEMP_BUILD=%~dp0build-temp

if not exist "!DEPS!" mkdir "!DEPS!"
if not exist "!TEMP_BUILD!" mkdir "!TEMP_BUILD!"

echo  -------------------------------------------------------
echo   Building tunnel.dll from source...
echo  -------------------------------------------------------
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0get-wireguard-dlls.ps1" -Deps "!TEMP_BUILD!" -Dist "!DEPS!"
if errorlevel 1 (
    echo.
    echo  ERROR: Build failed. Check output above.
    pause & exit /b 1
)

if not exist "!DEPS!\tunnel.dll" (
    echo  ERROR: tunnel.dll missing after build.
    pause & exit /b 1
)
if not exist "!DEPS!\wireguard.dll" (
    echo  ERROR: wireguard.dll missing after build.
    pause & exit /b 1
)

echo.
echo  ==========================================
echo   DLLs ready in wireguard-deps\
echo  ==========================================
echo.
echo   ..\wireguard-deps\tunnel.dll
echo   ..\wireguard-deps\wireguard.dll
echo.
echo   Run ..\BUILD.bat to compile and package MasselGUARD.
echo.
pause
exit /b 0
