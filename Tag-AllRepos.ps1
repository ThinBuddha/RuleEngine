param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [string]$Root = (Get-Location).Path,

    [string]$Message = "",

    [switch]$DryRun
)

if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = "Release $Tag"
}

Write-Host "Root: $Root"
Write-Host "Tag : $Tag"
Write-Host ""

$gitDirs = Get-ChildItem -Path $Root -Recurse -Force -Directory -Filter ".git"

$repos = $gitDirs |
    ForEach-Object { $_.Parent.FullName } |
    Sort-Object -Unique

foreach ($repo in $repos) {
    Write-Host "========================================"
    Write-Host "Repo: $repo"

    Push-Location $repo

    try {
        $remote = git remote get-url origin 2>$null

        if (-not $remote) {
            Write-Warning "Geen origin remote gevonden. Overslaan."
            continue
        }

        $existingTag = git tag --list $Tag

        if ($existingTag -eq $Tag) {
            Write-Warning "Tag '$Tag' bestaat lokaal al. Overslaan."
            continue
        }

        $status = git status --porcelain

        if ($status) {
            Write-Warning "Repo heeft lokale wijzigingen. Tag wordt gezet op huidige HEAD, maar check dit even."
        }

        $branch = git branch --show-current
        $commit = git rev-parse --short HEAD

        Write-Host "Branch : $branch"
        Write-Host "Commit : $commit"
        Write-Host "Remote : $remote"

        if ($DryRun) {
            Write-Host "[DRY RUN] git tag -a $Tag -m `"$Message`""
            Write-Host "[DRY RUN] git push origin $Tag"
        }
        else {
            git tag -a $Tag -m "$Message"

            if ($LASTEXITCODE -ne 0) {
                throw "Tag maken mislukt."
            }

            git push origin $Tag

            if ($LASTEXITCODE -ne 0) {
                throw "Pushen van tag mislukt."
            }

            Write-Host "Tag '$Tag' aangemaakt en gepusht."
        }
    }
    catch {
        Write-Error "Fout in repo '$repo': $_"
    }
    finally {
        Pop-Location
    }
}