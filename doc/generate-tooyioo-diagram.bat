@echo off
setlocal
pushd "%~dp0"

echo Generating Tooyioo event model diagram...
"%~dp0emhcl.exe" diagram "%~dp0event-model" -o "%~dp0tooyioo.em.html"
if errorlevel 1 (
    echo Diagram generation failed.
    popd
    pause
    exit /b 1
)

echo Diagram generated: %~dp0tooyioo.em.html
popd
pause
