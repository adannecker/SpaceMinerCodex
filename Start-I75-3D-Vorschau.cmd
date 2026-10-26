@echo off
setlocal
pushd "%~dp0"
powershell -NoProfile -File "%~dp0tools\Start-I75Viewer.ps1" -OpenBrowser
popd
