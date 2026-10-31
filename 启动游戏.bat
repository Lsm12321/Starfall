@echo off
rem =====================================================================
rem  Starfall - The Last Light : one-click launcher
rem
rem  Works from a fresh git clone. Builds the Windows player on demand,
rem  then starts it. Unity 6000.0.47f1 is required for building.
rem
rem  Usage: "%~nx0" [--rebuild] [--no-launch] [--help]
rem    --rebuild     force a fresh build even if the player looks current
rem    --no-launch   build / verify only, do not start the game
rem    --help        show this help
rem
rem  Set UNITY_EDITOR to the full path of Unity.exe to pin the editor.
rem  The resolved editor path is cached in work\unity-path.txt
rem  (work\ is gitignored, so this file stays a pure source-tree entry).
rem =====================================================================
setlocal EnableExtensions
title Starfall Launcher

set "SF_ROOT=%~dp0"
if "%SF_ROOT:~-1%"=="\" set "SF_ROOT=%SF_ROOT:~0,-1%"
set "SF_PROJECT=%SF_ROOT%"
set "SF_WORK=%SF_PROJECT%\work"
set "SF_OUT=%SF_PROJECT%\outputs\StarfallWindows"
set "SF_GAME=%SF_OUT%\Starfall.exe"
set "SF_STAMP=%SF_OUT%\.buildstamp"
set "SF_LOG=%SF_WORK%\launcher-build.log"
set "SF_HINT=%SF_WORK%\unity-path.txt"
set "SF_VER=6000.0.47f1"
set "SF_BUILD=0"
set "SF_LAUNCH=1"

rem ---------------------------------------------------------------- args
:parse
if "%~1"=="" goto args_done
if /i "%~1"=="--rebuild"   ( set "SF_BUILD=1"  & shift & goto parse )
if /i "%~1"=="--no-launch" ( set "SF_LAUNCH=0" & shift & goto parse )
if /i "%~1"=="--help"      ( goto usage )
if /i "%~1"=="-h"          ( goto usage )
if /i "%~1"=="/?"          ( goto usage )
echo Unknown option: %~1
echo.
goto usage

:args_done
if not exist "%SF_WORK%" mkdir "%SF_WORK%" >nul 2>&1

rem ------------------------------------------------- is a build needed?
if "%SF_BUILD%"=="1" goto need_build
if not exist "%SF_GAME%"  goto need_build
if not exist "%SF_OUT%\Starfall_Data" goto need_build
if not exist "%SF_STAMP%" goto need_build

rem Player exists. Rebuild only if a tracked source file is newer than the
rem build stamp. After a fresh clone the player does not exist at all, so
rem this branch is what makes repeat launches instant.
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$proj=$env:SF_PROJECT; $stamp=Get-Item -LiteralPath $env:SF_STAMP -ErrorAction SilentlyContinue; if(-not $stamp){ exit 1 }; $newest=$null; foreach($r in @('Assets','Packages','ProjectSettings')){ $p=Join-Path $proj $r; if(Test-Path -LiteralPath $p){ foreach($f in Get-ChildItem -LiteralPath $p -Force -Recurse -File -ErrorAction SilentlyContinue){ if(($null -eq $newest) -or ($f.LastWriteTimeUtc -gt $newest)){ $newest=$f.LastWriteTimeUtc } } } }; if(($null -ne $newest) -and ($newest -gt $stamp.LastWriteTimeUtc)){ exit 1 } else { exit 0 }"
if errorlevel 1 goto need_build
echo Player is up to date.
goto launch_or_exit

rem ------------------------------------------------- find the editor
:need_build
call :find_unity
if not exist "%SF_HINT%" goto no_unity
set "SF_UNITY="
for /f "usebackq delims=" %%P in ("%SF_HINT%") do set "SF_UNITY=%%P"
if not defined SF_UNITY goto no_unity
if not exist "%SF_UNITY%" goto no_unity

rem ------------------------------------------------- build
echo.
echo   Starfall - The Last Light
echo   ------------------------------------------------------------
echo   Unity    : %SF_UNITY%
echo   Version  : %SF_VER%
echo   Project  : %SF_PROJECT%
echo   Player   : %SF_GAME%
echo   Log      : %SF_LOG%
echo   ------------------------------------------------------------
echo.
echo   Building the Windows player...
echo   The first build in a fresh clone reimports every asset and can
echo   take several minutes. Do not close this window.
echo   Started: %TIME%
echo.

if not exist "%SF_OUT%" mkdir "%SF_OUT%" >nul 2>&1
if exist "%SF_WORK%\build-result.txt" del /q "%SF_WORK%\build-result.txt" >nul 2>&1

rem Refuse to rebuild while the same project is open in the GUI editor:
rem Unity would fail to open the project and the build would be bogus.
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$p=[IO.Path]::GetFullPath($env:SF_PROJECT).TrimEnd('\'); $r=Get-CimInstance Win32_Process -Filter 'Name=''Unity.exe''' -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($p,[StringComparison]::OrdinalIgnoreCase) -ge 0 }; if($r){ exit 1 } else { exit 0 }"
if errorlevel 1 goto EDITOR_OPEN

"%SF_UNITY%" -batchmode -nographics -quit -projectPath "%SF_PROJECT%" -executeMethod StarfallBuild.Build -logFile "%SF_LOG%"
set "SF_RC=%ERRORLEVEL%"
echo.
echo   Finished: %TIME%   (editor exit code %SF_RC%)

if "%SF_RC%"=="199" goto NO_LICENSE
if not "%SF_RC%"=="0" goto BUILD_FAILED
if not exist "%SF_GAME%" goto BUILD_FAILED
if not exist "%SF_OUT%\Starfall_Data" goto BUILD_FAILED
if not exist "%SF_WORK%\build-result.txt" goto BUILD_FAILED
findstr /b /c:"Succeeded" "%SF_WORK%\build-result.txt" >nul 2>&1
if errorlevel 1 goto BUILD_FAILED

rem Touch the stamp so the next run skips the build.
type nul > "%SF_STAMP%"
echo   Build succeeded.

rem ------------------------------------------------- launch
:launch_or_exit
if "%SF_LAUNCH%"=="0" goto done
if not exist "%SF_GAME%" goto BUILD_FAILED
echo.
echo   Launching "%SF_GAME%"
start "" /D "%SF_OUT%" "%SF_GAME%"
goto done

:done
endlocal
exit /b 0

rem =====================================================================
rem  Helper: resolve a Unity 6000.0.47f1 editor and cache it in SF_HINT.
rem  Returns 0 and writes the path on success, 1 when nothing matched.
rem  Version is verified via ProductVersion so a different Unity install
rem  (for example a 2022.3 editor next to it) can never be picked up.
rem =====================================================================
:find_unity
if exist "%SF_HINT%" (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$p=(Get-Content -LiteralPath $env:SF_HINT -Raw).Trim(); if($p -and (Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p).VersionInfo.ProductVersion -like ($env:SF_VER + '*'))){ exit 0 } else { exit 1 }"
    if not errorlevel 1 exit /b 0
    del /q "%SF_HINT%" >nul 2>&1
)
echo   Searching for Unity %SF_VER%...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$v=$env:SF_VER; $c=@(); if($env:UNITY_EDITOR){ $c+=$env:UNITY_EDITOR }; $hub=Join-Path $env:APPDATA 'UnityHub'; $e=Join-Path $hub 'editors-v2.json'; if(Test-Path -LiteralPath $e){ try{ foreach($x in (Get-Content -LiteralPath $e -Raw | ConvertFrom-Json).data){ if($x.version -eq $v){ foreach($l in $x.location){ $c+=$l } } } }catch{} }; $s=Join-Path $hub 'secondaryInstallPath.json'; if(Test-Path -LiteralPath $s){ try{ $b=(Get-Content -LiteralPath $s -Raw | ConvertFrom-Json); if($b){ $c+=(Join-Path $b ($v + '\Editor\Unity.exe')) } }catch{} }; foreach($r in @((Join-Path $env:ProgramFiles 'Unity\Hub\Editor'),(Join-Path ${env:ProgramFiles(x86)} 'Unity\Hub\Editor'),'C:\Unity\Hub\Editor','D:\Unity\Hub\Editor','E:\Unity\Hub\Editor','F:\Unity\Hub\Editor')){ $c+=(Join-Path $r ($v + '\Editor\Unity.exe')) }; $c+='E:\Unity\Editor\Unity.exe'; $c+='D:\Unity\Editor\Unity.exe'; $c+='C:\Unity\Editor\Unity.exe'; $c+=(Join-Path $env:ProgramFiles 'Unity\Editor\Unity.exe'); foreach($p in $c){ if($p -and (Test-Path -LiteralPath $p)){ $vi=(Get-Item -LiteralPath $p).VersionInfo; if($vi.ProductVersion -like ($v + '*')){ [IO.File]::WriteAllText($env:SF_HINT,$p); exit 0 } } }; exit 1"
if errorlevel 1 exit /b 1
exit /b 0

rem ------------------------------------------------- error exits
:no_unity
echo.
echo   Unity Editor %SF_VER% was not found on this computer.
echo.
echo   This project builds the game from source, so it needs that exact
echo   editor version. Options:
echo     1^) Install Unity %SF_VER% through Unity Hub, then run this again.
echo     2^) Set the UNITY_EDITOR environment variable to the full path of
echo        the Unity.exe you want to use.
echo     3^) Use a prebuilt release instead of building from source, if one
echo        is published for this project.
echo.
echo   Looked in: Unity Hub editors-v2.json / secondaryInstallPath,
echo   common Unity Hub folders and the usual manual install folders.
echo.
pause
exit /b 1

:BUILD_FAILED
echo.
echo   The build did not produce "%SF_GAME%".
if not exist "%SF_LOG%" goto BUILD_FAILED_NOLOG
findstr /i /c:"project is already open" /c:"already open in another instance" "%SF_LOG%" >nul 2>&1
if not errorlevel 1 goto EDITOR_OPEN
findstr /i /c:"No valid Unity Editor license" /c:"Unable to find a valid license" /c:"IPC channel to LicensingClient" /c:"LicensingClient doesn't exist" "%SF_LOG%" >nul 2>&1
if not errorlevel 1 goto NO_LICENSE
echo.
echo   Last 25 lines of the build log:
echo   ------------------------------------------------------------
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-Content -LiteralPath $env:SF_LOG -Tail 25 -ErrorAction SilentlyContinue"
echo   ------------------------------------------------------------
:BUILD_FAILED_NOLOG
echo.
echo   Full log: "%SF_LOG%"
echo   Result  : "%SF_WORK%\build-result.txt"
echo.
pause
exit /b 1

:NO_LICENSE
echo.
echo   Unity could not obtain a license, so the build was aborted.
echo   Unity exits with code 199 when its licensing client cannot start
echo   or cannot find a valid entitlement for this machine.
echo.
echo   Fix:
echo     1^) Open Unity Hub, sign in with the account that owns the
echo        license, and activate Unity %SF_VER% under Licenses.
echo     2^) Check that this user can read machine information: the
echo        licensing client needs WMI access ^(BIOS, network adapters^)
echo        to build the device fingerprint. Blocked WMI or a heavily
echo        restricted sandbox makes it report "no licenses" even when
echo        a valid license file exists.
echo     3^) Then run this launcher again.
echo.
echo   Full log: "%SF_LOG%"
echo.
pause
exit /b 1

:EDITOR_OPEN
echo.
echo   This project is already open in the Unity Editor.
echo   Save your work, close the Editor, then run this launcher again.
echo   The Editor was left untouched and nothing was rebuilt.
echo.
pause
exit /b 1

:usage
echo.
echo   Starfall - The Last Light : one-click launcher
echo.
echo   Usage: "%~nx0" [options]
echo.
echo     --rebuild     force a fresh build even if the player looks current
echo     --no-launch   build or verify only, do not start the game
echo     --help        show this help
echo.
echo   With no options it builds only when needed, then starts the game.
echo.
exit /b 0
