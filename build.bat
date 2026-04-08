@echo off
REM Simple build script for DataverseFakes

echo.
echo ===================================
echo   DataverseFakes Build Script
echo ===================================
echo.

if "%1"=="clean" goto clean
if "%1"=="restore" goto restore
if "%1"=="build" goto build
if "%1"=="test" goto test
if "%1"=="pack" goto pack
if "%1"=="" goto default

:help
echo Usage: build.bat [command]
echo.
echo Commands:
echo   clean    - Clean build artifacts
echo   restore  - Restore NuGet packages
echo   build    - Build the solution
echo   test     - Run tests
echo   pack     - Create NuGet package
echo   (none)   - Run restore, build, and test
echo.
goto end

:clean
echo Cleaning build artifacts...
if exist build rmdir /s /q build
if exist test rmdir /s /q test
if exist nuget rmdir /s /q nuget
if exist Publish rmdir /s /q Publish
echo Clean complete.
if "%1"=="clean" goto end
goto restore

:restore
echo Restoring NuGet packages...
dotnet restore DataverseFakes.sln
if errorlevel 1 (
    echo ERROR: NuGet restore failed
    exit /b 1
)
echo Restore complete.
if "%1"=="restore" goto end
goto build

:build
echo Building solution...
dotnet build DataverseFakes.sln --configuration Release --no-restore
if errorlevel 1 (
    echo ERROR: Build failed
    exit /b 1
)
echo Build complete.
if "%1"=="build" goto end
goto test

:test
echo Running tests...
dotnet test DataverseFakes.Tests\DataverseFakes.Tests.csproj --configuration Release --no-build --verbosity normal
if errorlevel 1 (
    echo ERROR: Tests failed
    exit /b 1
)
echo Tests complete.
if "%1"=="test" goto end
goto end

:pack
echo Creating NuGet package...
if not exist nuget mkdir nuget
dotnet pack DataverseFakes\DataverseFakes.csproj --configuration Release --output nuget --no-build
if errorlevel 1 (
    echo ERROR: Pack failed
    exit /b 1
)
echo Pack complete.
goto end

:default
echo Running default build pipeline: restore, build, test...
call :restore
call :build
call :test
echo.
echo ===================================
echo   Build Complete!
echo ===================================
goto end

:end
