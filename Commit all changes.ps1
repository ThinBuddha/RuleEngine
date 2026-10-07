$root = "C:\Projects\VisualStudio\Code\RuleEngine"

Get-ChildItem -Path $root -Directory |
Where-Object { $_.Name -like "*" } |
ForEach-Object {
    $repoPath = $_.FullName
    $gitFolder = Join-Path $repoPath ".git"

    if (-not (Test-Path $gitFolder)) {
        Write-Host "SKIP (geen git repo): $repoPath" -ForegroundColor DarkYellow
        return
    }

    Write-Host ""
    Write-Host "=== Checking $repoPath ===" -ForegroundColor Cyan

    Push-Location $repoPath
    try {
        $status = git status --porcelain 2>$null

        if ([string]::IsNullOrWhiteSpace(($status -join ""))) {
            Write-Host "Geen wijzigingen, skip." -ForegroundColor DarkGray
            return
        }

        Write-Host "Wijzigingen gevonden. Committen..." -ForegroundColor Yellow

        git add -A
        if ($LASTEXITCODE -ne 0) {
            Write-Host "git add mislukt in $repoPath" -ForegroundColor Red
            return
        }

        git commit -m "Ongoing"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "git commit mislukt in $repoPath" -ForegroundColor Red
            return
        }

        git push
        if ($LASTEXITCODE -ne 0) {
            $branch = git rev-parse --abbrev-ref HEAD 2>$null
            if (-not [string]::IsNullOrWhiteSpace($branch)) {
                Write-Host "Normale push mislukte, probeer upstream te zetten..." -ForegroundColor Yellow
                git push -u origin $branch
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "git push -u origin $branch mislukt in $repoPath" -ForegroundColor Red
                }
                else {
                    Write-Host "Push gelukt met upstream." -ForegroundColor Green
                }
            }
            else {
                Write-Host "Kon huidige branch niet bepalen in $repoPath" -ForegroundColor Red
            }
        }
        else {
            Write-Host "Push gelukt." -ForegroundColor Green
        }
    }
    finally {
        Pop-Location
    }
}