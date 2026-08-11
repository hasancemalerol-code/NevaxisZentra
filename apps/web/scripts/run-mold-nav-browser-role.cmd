@echo off
setlocal
set ROOT=%~dp0..\..\..
set WEB=%ROOT%\apps\web
set PLAY=%TEMP%\mold-closure-playwright
set SUBJECT=%1
set ROLE=%2
if "%SUBJECT%"=="" exit /b 1
if "%ROLE%"=="" exit /b 1

for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":3001" ^| findstr "LISTENING"') do taskkill /PID %%a /F >nul 2>&1
timeout /t 2 /nobreak >nul
start "" /B cmd /c "cd /d %WEB% && set CREVAFACTOS_DEV_AUTH_SUBJECT=%SUBJECT%&& npx next dev -p 3001"

set /a WAIT=0
:waitweb
timeout /t 2 /nobreak >nul
powershell -NoProfile -Command "try { (Invoke-WebRequest -Uri http://localhost:3001/dashboard -UseBasicParsing).StatusCode } catch { 0 }" | findstr "200" >nul && goto runbrowser
set /a WAIT+=1
if %WAIT% GEQ 45 exit /b 1
goto waitweb

:runbrowser
cd /d %PLAY%
set MOLD_BROWSER_ROLE=%ROLE%
set MOLD_BROWSER_BASE_URL=http://localhost:3001
node verify-mold-1-nav-restart-browser.mjs
set ERR=%ERRORLEVEL%

for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":3001" ^| findstr "LISTENING"') do taskkill /PID %%a /F >nul 2>&1
exit /b %ERR%
