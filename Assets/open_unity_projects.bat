@echo off
setlocal ENABLEEXTENSIONS

rem ================================================================
rem Open Unity project roots (folders that contain "Assets")
rem - For each directory that directly contains an "Assets" subfolder:
rem     * open that folder as a Unity project
rem     * do NOT recurse further into that project's subtree
rem - Other directories are traversed to keep searching.
rem
rem USAGE:
rem   open-unity-projects.bat 2022.3.18f1
rem   open-unity-projects.bat "C:\Program Files\Unity\Hub\Editor\2022.3.18f1\Editor\Unity.exe"
rem   open-unity-projects.bat 2022.3.18f1 /wait
rem   open-unity-projects.bat 2022.3.18f1 /dryrun
rem   open-unity-projects.bat 2022.3.18f1 /root "D:\Projects"
rem
rem Options:
rem   /wait    Open projects one at a time. The script waits until Unity closes
rem            before opening the next project.
rem   /dryrun  Show the Unity commands that would be executed, but open nothing.
rem   /root    Start scanning from the provided folder instead of the current folder.
rem
rem Unity argument:
rem   First argument may be either:
rem     * a Unity version installed through Unity Hub, e.g. 2022.3.18f1
rem     * a full path to Unity.exe
rem     * a Unity editor install folder, e.g.
rem       C:\Program Files\Unity\Hub\Editor\2022.3.18f1
rem ================================================================

set "ROOT=%cd%"
set "UNITY_ARG=%~1"
set "UNITY_EXE="
set "WAIT="
set "DRYRUN="
set "COUNT=0"

if not defined UNITY_ARG goto :Usage
shift /1

call :ResolveUnity "%UNITY_ARG%"
if not defined UNITY_EXE (
  echo [ERROR] Could not resolve Unity from: "%UNITY_ARG%"
  echo.
  echo Provide either a Unity version installed through Unity Hub or a full path to Unity.exe.
  echo Example:
  echo   %~nx0 2022.3.18f1
  echo   %~nx0 "C:\Program Files\Unity\Hub\Editor\2022.3.18f1\Editor\Unity.exe"
  exit /b 1
)

:ParseOptions
if "%~1"=="" goto :OptionsDone
if /I "%~1"=="/wait" (
  set "WAIT=1"
  shift /1
  goto :ParseOptions
)
if /I "%~1"=="/dryrun" (
  set "DRYRUN=1"
  shift /1
  goto :ParseOptions
)
if /I "%~1"=="/root" (
  shift /1
  if "%~1"=="" (
    echo [ERROR] /root requires a folder path.
    exit /b 1
  )
  set "ROOT=%~1"
  shift /1
  goto :ParseOptions
)

echo [ERROR] Unknown option: %~1
exit /b 1

:OptionsDone
if not exist "%ROOT%\" (
  echo [ERROR] Root folder does not exist: "%ROOT%"
  exit /b 1
)

echo.
echo ================================================================
echo   Scanning from: "%ROOT%"
echo   Unity executable: "%UNITY_EXE%"
echo   Will open directories that contain an immediate "Assets" subfolder.
echo ================================================================
if defined WAIT echo [WAIT MODE] Projects will open one at a time.
if defined DRYRUN echo [DRY RUN] Nothing will be opened.

call :ProcessDir "%ROOT%"

echo.
echo [DONE] Found %COUNT% Unity project(s).
exit /b 0


:Usage
echo.
echo USAGE:
echo   %~nx0 ^<UnityVersionOrUnityExePath^> [/wait] [/dryrun] [/root "D:\Projects"]
echo.
echo Examples:
echo   %~nx0 2022.3.18f1
echo   %~nx0 "C:\Program Files\Unity\Hub\Editor\2022.3.18f1\Editor\Unity.exe"
echo   %~nx0 2022.3.18f1 /wait
echo   %~nx0 2022.3.18f1 /root "D:\Unity Projects"
exit /b 1


:ResolveUnity
rem %~1 = Unity version, path to Unity.exe, or editor install folder
set "ARG=%~1"

rem Full path to Unity.exe
if exist "%ARG%" (
  for %%F in ("%ARG%") do (
    if /I "%%~nxF"=="Unity.exe" set "UNITY_EXE=%%~fF"
  )
)
if defined UNITY_EXE goto :eof

rem Editor install folder ending at the version folder:
rem C:\Program Files\Unity\Hub\Editor\2022.3.18f1
if exist "%ARG%\Editor\Unity.exe" (
  set "UNITY_EXE=%ARG%\Editor\Unity.exe"
  goto :eof
)

rem Editor folder itself:
rem C:\Program Files\Unity\Hub\Editor\2022.3.18f1\Editor
if exist "%ARG%\Unity.exe" (
  set "UNITY_EXE=%ARG%\Unity.exe"
  goto :eof
)

rem Unity Hub default install locations by version
if exist "%ProgramFiles%\Unity\Hub\Editor\%ARG%\Editor\Unity.exe" (
  set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\%ARG%\Editor\Unity.exe"
  goto :eof
)

if exist "%ProgramFiles(x86)%\Unity\Hub\Editor\%ARG%\Editor\Unity.exe" (
  set "UNITY_EXE=%ProgramFiles(x86)%\Unity\Hub\Editor\%ARG%\Editor\Unity.exe"
  goto :eof
)

goto :eof


:ProcessDir
rem %~1 = current directory to inspect
set "CUR=%~1"
if not exist "%CUR%\" goto :eof

rem Is this a Unity project root? Same rule as the cleanup script:
rem it has an immediate "Assets" subfolder.
if exist "%CUR%\Assets\" (
  set /a COUNT+=1
  echo.
  echo --- Unity project root: "%CUR%" has Assets ---
  call :OpenProject "%CUR%"

  rem Do NOT recurse further into this subtree.
  goto :eof
)

rem Otherwise, recurse into subdirectories to keep searching.
for /D %%S in ("%CUR%\*") do (
  if exist "%%~fS\" call :ProcessDir "%%~fS"
)
goto :eof


:OpenProject
rem %~1 = Unity project root
set "PROJECT=%~1"

if defined DRYRUN (
  echo start "" "%UNITY_EXE%" -projectPath "%PROJECT%"
  goto :eof
)

if defined WAIT (
  echo Opening and waiting: "%PROJECT%"
  "%UNITY_EXE%" -projectPath "%PROJECT%"
) else (
  echo Opening: "%PROJECT%"
  start "" "%UNITY_EXE%" -projectPath "%PROJECT%"
)

goto :eof
