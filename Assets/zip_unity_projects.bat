@echo off
setlocal ENABLEEXTENSIONS DISABLEDELAYEDEXPANSION

rem ================================================================
rem Archive Unity project roots as .zip files using 7-Zip
rem
rem Detection/traversal rule:
rem   - A Unity project root is any folder that directly contains an
rem     "Assets" subfolder.
rem   - Once such a folder is found, the script does NOT recurse into it.
rem
rem Archive behavior:
rem   - For each project folder named:
rem       D:\Somewhere\unity_project
rem     the script creates:
rem       D:\Somewhere\unity_project.zip
rem   - The zip contains these folders directly at the archive root:
rem       Assets
rem       Packages          if present
rem       ProjectSettings   if present
rem   - If unity_project.zip already exists, it is deleted first.
rem
rem Optional deletion behavior:
rem   - By default, original Unity project folders are NOT deleted.
rem   - Add /delete-original /yes to permanently delete original project
rem     folders after their zip was created successfully.
rem   - Permanent deletion uses rmdir /s /q and bypasses the Recycle Bin.
rem
rem USAGE:
rem   zip_unity_projects.bat [/root "D:\Projects"] [/7z "C:\Program Files\7-Zip\7z.exe"] [/dryrun]
rem   zip_unity_projects.bat [/root "D:\Projects"] [/7z "C:\Program Files\7-Zip\7z.exe"] /delete-original /yes
rem
rem Examples:
rem   zip_unity_projects.bat /root "D:\Unity Projects"
rem   zip_unity_projects.bat /root "D:\Unity Projects" /dryrun
rem   zip_unity_projects.bat /root "D:\Unity Projects" /7z "C:\Program Files\7-Zip\7z.exe"
rem   zip_unity_projects.bat /root "D:\Unity Projects" /delete-original /yes
rem ================================================================

set "ROOT=%cd%"
set "SEVENZIP="
set "DRYRUN="
set "DELETE_ORIGINAL="
set "CONFIRMED="

set "COUNT=0"
set "ZIPPED=0"
set "FAILED=0"
set "EXISTING_ZIPS_DELETED=0"
set "ORIGINALS_DELETED=0"
set "ORIGINALS_SKIPPED=0"

:ParseOptions
if "%~1"=="" goto :OptionsDone

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

if /I "%~1"=="/7z" (
  shift /1
  if "%~1"=="" (
    echo [ERROR] /7z requires a path to 7z.exe.
    exit /b 1
  )
  set "SEVENZIP=%~1"
  shift /1
  goto :ParseOptions
)

if /I "%~1"=="/dryrun" (
  set "DRYRUN=1"
  shift /1
  goto :ParseOptions
)

if /I "%~1"=="/delete-original" (
  set "DELETE_ORIGINAL=1"
  shift /1
  goto :ParseOptions
)

if /I "%~1"=="/yes" (
  set "CONFIRMED=1"
  shift /1
  goto :ParseOptions
)

if /I "%~1"=="/help" goto :Usage
if /I "%~1"=="/h" goto :Usage
if /I "%~1"=="/?" goto :Usage

echo [ERROR] Unknown option: %~1
echo.
goto :Usage

:OptionsDone
if not exist "%ROOT%\" (
  echo [ERROR] Root folder does not exist: "%ROOT%"
  exit /b 1
)

rem Convert ROOT to a full path.
for %%I in ("%ROOT%") do set "ROOT=%%~fI"

if not defined SEVENZIP call :ResolveSevenZip
if not defined SEVENZIP (
  echo [ERROR] Could not find 7z.exe.
  echo         Install 7-Zip, add 7z.exe to PATH, or pass it explicitly:
  echo         %~nx0 /7z "C:\Program Files\7-Zip\7z.exe"
  exit /b 1
)

if not exist "%SEVENZIP%" (
  echo [ERROR] 7z.exe was not found at: "%SEVENZIP%"
  exit /b 1
)

if defined DELETE_ORIGINAL if not defined CONFIRMED (
  echo [ERROR] /delete-original requires /yes because it permanently deletes project folders.
  echo         This bypasses the Recycle Bin.
  exit /b 1
)

echo.
echo ================================================================
echo   Scanning from: "%ROOT%"
echo   7-Zip:        "%SEVENZIP%"
echo   Finds folders that contain an immediate "Assets" subfolder.
echo ================================================================

if defined DRYRUN (
  echo [DRY RUN] No zip files will be created or deleted.
) else (
  echo [ZIP MODE] Existing .zip files next to projects will be deleted first.
  echo [ZIP MODE] New .zip files will contain Assets, Packages, ProjectSettings directly.
)

if defined DELETE_ORIGINAL (
  echo [DELETE ORIGINALS] Original project folders will be permanently deleted after successful zipping.
  echo [WARNING] This bypasses the Recycle Bin.
) else (
  echo [KEEP ORIGINALS] Original project folders will not be deleted.
)

rem Move the script's current working directory outside ROOT where possible.
rem This helps if ROOT itself is a Unity project folder and /delete-original is used.
for %%I in ("%ROOT%\..") do set "ROOT_PARENT=%%~fI"
pushd "%ROOT_PARENT%" >nul 2>nul
if errorlevel 1 pushd "%TEMP%" >nul 2>nul

call :ProcessDir "%ROOT%"

popd >nul 2>nul

echo.
echo [DONE] Found Unity projects:      %COUNT%
echo [DONE] Created zip files:         %ZIPPED%
echo [DONE] Deleted existing zip files:%EXISTING_ZIPS_DELETED%
echo [DONE] Failed:                    %FAILED%
if defined DELETE_ORIGINAL (
  echo [DONE] Deleted originals:        %ORIGINALS_DELETED%
  echo [DONE] Skipped originals:        %ORIGINALS_SKIPPED%
)

if not "%FAILED%"=="0" exit /b 2
exit /b 0


:Usage
echo.
echo USAGE:
echo   %~nx0 [/root "D:\Projects"] [/7z "C:\Program Files\7-Zip\7z.exe"] [/dryrun]
echo   %~nx0 [/root "D:\Projects"] [/7z "C:\Program Files\7-Zip\7z.exe"] /delete-original /yes
echo.
echo Examples:
echo   %~nx0 /root "D:\Unity Projects"
echo   %~nx0 /root "D:\Unity Projects" /dryrun
echo   %~nx0 /root "D:\Unity Projects" /7z "C:\Program Files\7-Zip\7z.exe"
echo   %~nx0 /root "D:\Unity Projects" /delete-original /yes
echo.
echo Notes:
echo   The zip is created next to the project folder.
echo   Existing zip files with the same name are deleted first.
echo   Zip contents are Assets, Packages, ProjectSettings directly at archive root.
echo   Original project folders are only deleted with /delete-original /yes.
exit /b 1


:ResolveSevenZip
if defined ProgramFiles if exist "%ProgramFiles%\7-Zip\7z.exe" (
  set "SEVENZIP=%ProgramFiles%\7-Zip\7z.exe"
  goto :eof
)

if not "%ProgramFiles(x86)%"=="" if exist "%ProgramFiles(x86)%\7-Zip\7z.exe" (
  set "SEVENZIP=%ProgramFiles(x86)%\7-Zip\7z.exe"
  goto :eof
)

for /f "usebackq delims=" %%Z in (`where 7z.exe 2^>nul`) do (
  if not defined SEVENZIP set "SEVENZIP=%%~fZ"
)
goto :eof


:ProcessDir
rem %~1 = current directory to inspect
set "CUR=%~1"
if not exist "%CUR%\" goto :eof

rem Same detection rule as before: a project has an immediate "Assets" subfolder.
if exist "%CUR%\Assets\" (
  set /a COUNT+=1
  echo.
  echo --- Unity project root: "%CUR%" has Assets ---
  call :ZipProject "%CUR%"

  rem Do NOT recurse further into this subtree.
  goto :eof
)

rem Otherwise, recurse into subdirectories to keep searching.
for /D %%S in ("%CUR%\*") do (
  if exist "%%~fS\" call :ProcessDir "%%~fS"
)
goto :eof


:ZipProject
rem %~1 = Unity project root
set "PROJECT=%~f1"
if not exist "%PROJECT%\" goto :eof

for %%I in ("%PROJECT%") do (
  set "PROJECT_NAME=%%~nxI"
  set "PROJECT_PARENT=%%~dpI"
)

set "ZIP_FILE=%PROJECT_PARENT%%PROJECT_NAME%.zip"

echo Zip target: "%ZIP_FILE%"

if not exist "%PROJECT%\Packages\" (
  echo [WARN] Missing Packages folder; archive will contain Assets and any other present required folders.
)
if not exist "%PROJECT%\ProjectSettings\" (
  echo [WARN] Missing ProjectSettings folder; archive may not reopen as a complete Unity project.
)

if defined DRYRUN (
  if exist "%ZIP_FILE%" echo [DRY RUN] Would delete existing zip: "%ZIP_FILE%"
  echo [DRY RUN] Would create zip from: "%PROJECT%"
  echo           Contents at zip root: Assets, Packages if present, ProjectSettings if present.
  if defined DELETE_ORIGINAL echo [DRY RUN] Would permanently delete original after successful zip: "%PROJECT%"
  goto :eof
)

if exist "%ZIP_FILE%" (
  echo Deleting existing zip: "%ZIP_FILE%"
  del /f /q "%ZIP_FILE%" >nul 2>nul
  if exist "%ZIP_FILE%" (
    echo [ERROR] Could not delete existing zip: "%ZIP_FILE%"
    set /a FAILED+=1
    goto :eof
  )
  set /a EXISTING_ZIPS_DELETED+=1
)

set "LIST_FILE=%TEMP%\unity_zip_list_%RANDOM%_%RANDOM%.txt"
> "%LIST_FILE%" echo Assets
if exist "%PROJECT%\Packages\" >> "%LIST_FILE%" echo Packages
if exist "%PROJECT%\ProjectSettings\" >> "%LIST_FILE%" echo ProjectSettings

pushd "%PROJECT%" >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Could not enter project folder: "%PROJECT%"
  del /f /q "%LIST_FILE%" >nul 2>nul
  set /a FAILED+=1
  goto :eof
)

"%SEVENZIP%" a -tzip "%ZIP_FILE%" @"%LIST_FILE%"
set "ZIP_RESULT=%ERRORLEVEL%"

popd >nul 2>nul
del /f /q "%LIST_FILE%" >nul 2>nul

if not "%ZIP_RESULT%"=="0" (
  echo [ERROR] 7-Zip failed with exit code %ZIP_RESULT% for: "%PROJECT%"
  if exist "%ZIP_FILE%" (
    echo [INFO] Removing partial zip: "%ZIP_FILE%"
    del /f /q "%ZIP_FILE%" >nul 2>nul
  )
  set /a FAILED+=1
  goto :eof
)

if not exist "%ZIP_FILE%" (
  echo [ERROR] 7-Zip reported success but zip file was not found: "%ZIP_FILE%"
  set /a FAILED+=1
  goto :eof
)

echo [OK] Created zip: "%ZIP_FILE%"
set /a ZIPPED+=1

if defined DELETE_ORIGINAL call :DeleteOriginalProject "%PROJECT%"

goto :eof


:DeleteOriginalProject
rem %~1 = Unity project root, only called after successful zip
set "PROJECT=%~f1"
if not exist "%PROJECT%\" goto :eof

call :IsDangerousDeleteTarget "%PROJECT%"
if errorlevel 1 (
  echo [SKIP] Refusing to delete dangerous original project folder: "%PROJECT%"
  set /a ORIGINALS_SKIPPED+=1
  goto :eof
)

echo Deleting original project permanently: "%PROJECT%"

rem Remove attributes that can otherwise block deletion in some folders.
attrib -r -s -h "%PROJECT%" /s /d >nul 2>nul

rem rmdir /s /q permanently deletes and bypasses the Recycle Bin.
rmdir /s /q "%PROJECT%"

if exist "%PROJECT%\" (
  echo [ERROR] Failed to delete original project folder: "%PROJECT%"
  echo         It may contain locked files, require elevated permissions, or be open in Unity/another app.
  set /a FAILED+=1
) else (
  echo [OK] Deleted original project folder: "%PROJECT%"
  set /a ORIGINALS_DELETED+=1
)

goto :eof


:IsDangerousDeleteTarget
rem Returns errorlevel 1 for targets that should never be deleted by this script.
set "TARGET=%~f1"

rem Refuse drive roots, e.g. C:\ or D:\
for %%D in ("%TARGET%") do (
  if /I "%%~fD"=="%%~dD\" exit /b 1
)

rem Refuse common Windows/system roots.
if /I "%TARGET%"=="%SystemDrive%\" exit /b 1
if defined SystemRoot if /I "%TARGET%"=="%SystemRoot%" exit /b 1
if defined ProgramFiles if /I "%TARGET%"=="%ProgramFiles%" exit /b 1
if not "%ProgramFiles(x86)%"=="" if /I "%TARGET%"=="%ProgramFiles(x86)%" exit /b 1
if defined USERPROFILE if /I "%TARGET%"=="%USERPROFILE%" exit /b 1

exit /b 0
