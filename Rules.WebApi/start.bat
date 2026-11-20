@echo off
start powershell -Command "Set-Location 'C:\Projects\VisualStudio\Code\RuleEngine\Rules.WebApi'; dotnet run"
start powershell -Command "Set-Location 'C:\Projects\VisualStudio\Code\RuleEngine\Rules.WebApi.GUI'; npm run dev"