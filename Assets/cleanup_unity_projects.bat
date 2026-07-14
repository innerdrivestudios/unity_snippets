@echo off
setlocal ENABLEEXTENSIONS

rem ================================================================
rem Clean Unity project roots (folders that contain "Assets")
rem - For each directory that directly contains an "Assets" subfolder:
rem     * delete sibling "Library" and "Logs" folders (ONLY those siblings)
rem     * delete sibling files: .vsconfig, *.sln, *.csproj (in that folder only)
rem - Other directories are just traversed to keep searching.
rem
rem USAGE:
rem   clean-unity-roots.bat           (dry run; shows actions)
rem   clean-unity-roots.bat /force    (actually deletes)
rem ================================================================

set "ROOT=%cd%"
set "FORCE="
if "%~1"=="/force" set "FORCE=1"

echo.
echo ================================================================
echo   Scanning from: "%ROOT%"
echo   Will clean ONLY directories that contain an "Assets" subfolder
echo   Targets at each project root:
echo     - Folders: "Library", "Logs" "obj" "Temp" ".vs" (siblings of Assets)
echo     - Files:   ".vsconfig", "*.sln", "*.csproj" (in root only)
echo ================================================================
if not defined FORCE (
  echo [DRY RUN] Nothing will be deleted. Use /force to actually delete.
)

call :ProcessDir "%ROOT%"

echo.
if defined FORCE (
  echo [DONE] Cleanup complete.
) else (
  echo [DONE] Dry run complete. Nothing deleted. Rerun with /force to delete the listed files.
)
exit /b

:ProcessDir
rem %~1 = current directory to inspect
set "CUR=%~1"
if not exist "%CUR%\" goto :eof

rem Is this a Unity project root (has an immediate "Assets" subfolder)?
if exist "%CUR%\Assets\" (
  echo.
  echo --- Unity project root: "%CUR%" has Assets ---

  rem Delete sibling folders (ONLY at this level)
  for %%D in ("Library" "Logs" ".vs" "obj" "Temp" ".idea" "__MACOSX") do (
    if exist "%CUR%\%%~D\" (
      if defined FORCE (
        call :SafeRmDir "%CUR%\%%~D"
      ) else (
        echo rmdir /s /q "%CUR%\%%~D"
      )
    )
  )

	rem Delete sibling files (ONLY at this project root)
	if exist "%CUR%\.vsconfig" (
	  if defined FORCE (del /f /q "%CUR%\.vsconfig" 2>nul) else echo del /f /q "%CUR%\.vsconfig"
	)

	if exist "%CUR%\*.sln" (
	  if defined FORCE (del /f /q "%CUR%\*.sln" 2>nul) else echo del /f /q "%CUR%\*.sln"
	)

	if exist "%CUR%\*.csproj" (
	  if defined FORCE (del /f /q "%CUR%\*.csproj" 2>nul) else echo del /f /q "%CUR%\*.csproj"
	)


  rem Do NOT recurse further into this subtree (we already handled its root).
  goto :eof
)

rem Otherwise, recurse into subdirectories to keep searching.
for /D %%S in ("%CUR%\*") do (
  if exist "%%~fS\" call :ProcessDir "%%~fS"
)
goto :eof


:: Safe, quiet directory deletion with long-path support
:: Usage: call :SafeRmDir "D:\very\long\path\Library"
:SafeRmDir
set "TARGET=%~1"
if not exist "%TARGET%\" goto :eof

rem 1) Try normal RD first (fast path)
rmdir /s /q "%TARGET%" 2>nul
if not exist "%TARGET%\" goto :eof

rem 2) Try long-path RD using \\?\ prefix
set "XLP=\\?\%TARGET%"
rmdir /s /q "%XLP%" 2>nul
if not exist "%TARGET%\" goto :eof

rem 3) Powershell (supports long paths; quiet)
powershell -NoProfile -Command ^
  "Remove-Item -LiteralPath '%TARGET%' -Recurse -Force -ErrorAction SilentlyContinue"
if not exist "%TARGET%\" goto :eof

rem 4) Robust fallback: robocopy mirror to empty, then remove
set "EMPTY=%TEMP%\__emptydir__"
mkdir "%EMPTY%" 2>nul
robocopy "%EMPTY%" "%TARGET%" /MIR >nul 2>&1
rmdir /s /q "%TARGET%" 2>nul
rmdir "%EMPTY%" 2>nul
goto :eof
