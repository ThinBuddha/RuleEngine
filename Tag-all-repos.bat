@echo off
set /p TAG=Welke tag wil je zetten? 
powershell.exe -NoExit -ExecutionPolicy Bypass -File "C:\Projects\VisualStudio\Code\RuleEngine\Tag-AllRepos.ps1" -Root "C:\Projects\VisualStudio\Code\RuleEngine" -Tag "%TAG%"
pause