@REM Snapmaker_Orca build script for Windows (VS 2026 / Dev18)
@echo off
for %%I in ("%~dp0.") do set "WP=%%~fI"

@REM Resolve cmake.exe (prefer PATH, fallback to Visual Studio bundled CMake)
set "CMAKE_EXE="
where cmake >nul 2>nul
if errorlevel 1 goto :resolve_cmake_vs
set "CMAKE_EXE=cmake"
goto :cmake_ok

:resolve_cmake_vs
set "VSROOT="
for /f "delims=" %%i in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -property installationPath') do set "VSROOT=%%i"
if "%VSROOT%"=="" goto :cmake_fail
set "CMAKE_EXE=%VSROOT%\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
if exist "%CMAKE_EXE%" goto :cmake_ok

:cmake_fail
echo ERROR: cmake.exe not found. Install "CMake tools for Windows" or CMake for Windows.
exit /b 1

:cmake_ok

@REM Pack deps
if "%1"=="pack" (
    setlocal ENABLEDELAYEDEXPANSION
    cd /d "%WP%\deps\build"
    for /f "tokens=2-4 delims=/ " %%a in ('date /t') do set build_date=%%c%%b%%a
    echo packing deps: OrcaSlicer_dep_win64_!build_date!_vs2026.zip

    %WP%/tools/7z.exe a OrcaSlicer_dep_win64_!build_date!_vs2026.zip OrcaSlicer_dep
    exit /b 0
)

set debug=OFF
set debuginfo=OFF
if "%1"=="debug" set debug=ON
if "%2"=="debug" set debug=ON
if "%1"=="debuginfo" set debuginfo=ON
if "%2"=="debuginfo" set debuginfo=ON
if "%debug%"=="ON" (
    set build_type=Debug
    set build_dir=build-dbg
) else (
    if "%debuginfo%"=="ON" (
        set build_type=RelWithDebInfo
        set build_dir=build-dbginfo
    ) else (
        set build_type=Release
        set build_dir=build
    )
)
echo build type set to %build_type%

setlocal DISABLEDELAYEDEXPANSION
cd /d "%WP%\deps"
mkdir "%build_dir%" 2>nul
cd /d "%WP%\deps\%build_dir%"
set DEPS=%CD%/OrcaSlicer_dep

if "%1"=="slicer" (
    GOTO :slicer
)
echo "building deps.."

echo on
"%CMAKE_EXE%" ../ -G "Visual Studio 18 2026" -A x64 -DDESTDIR="%DEPS%" -DCMAKE_BUILD_TYPE=%build_type% -DDEP_DEBUG=%debug% -DORCA_INCLUDE_DEBUG_INFO=%debuginfo%
"%CMAKE_EXE%" --build . --config %build_type% --target deps -- -m
@echo off

if "%1"=="deps" exit /b 0

:slicer
echo "building Snapmaker Orca..."
cd /d "%WP%"
mkdir "%build_dir%" 2>nul
cd /d "%WP%\\%build_dir%"

echo on
"%CMAKE_EXE%" .. -G "Visual Studio 18 2026" -A x64 -DBBL_RELEASE_TO_PUBLIC=1 -DCMAKE_PREFIX_PATH="%DEPS%/usr/local" -DOpenCV_DIR="%DEPS%/usr/local/staticlib" -DCMAKE_INSTALL_PREFIX="./Snapmaker_Orca" -DCMAKE_BUILD_TYPE=%build_type% -DCMAKE_POLICY_VERSION_MINIMUM=3.5
"%CMAKE_EXE%" --build . --config %build_type% --target ALL_BUILD -- -m
@echo off
cd ..
call run_gettext.bat
cd %build_dir%
"%CMAKE_EXE%" --build . --target install --config %build_type%
