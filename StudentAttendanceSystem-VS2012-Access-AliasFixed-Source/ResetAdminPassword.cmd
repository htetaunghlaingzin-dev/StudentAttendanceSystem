@echo off
"%SystemRoot%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0ResetAdminPassword.ps1' -Username admin -NewPassword (Read-Host 'Enter new admin password (minimum 8 characters)' -AsSecureString)"
echo.
pause
