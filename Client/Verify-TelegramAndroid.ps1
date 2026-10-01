$ErrorActionPreference = "Stop"

$root = Join-Path $PSScriptRoot "TelegramAndroid"

if (-not (Test-Path (Join-Path $root ".git"))) {
    throw "Telegram Android source was not found at: $root"
}

$required = @(
    "TMessagesProj",
    "TMessagesProj_App",
    "build.gradle",
    "gradle.properties",
    "gradlew.bat"
)

foreach ($item in $required) {
    if (-not (Test-Path (Join-Path $root $item))) {
        throw "Required Android source item is missing: $item"
    }
}

Push-Location $root
try {
    $commit = (git rev-parse HEAD).Trim()
    $dirty = git status --porcelain
    $submodules = git submodule status --recursive

    Write-Host "Telegram Android commit: $commit"
    Write-Host ""
    Write-Host "Submodule status:"
    $submodules | ForEach-Object { Write-Host $_ }

    $bad = @($submodules | Where-Object { $_ -match "^[+-]" })
    if ($bad.Count -gt 0) {
        throw "One or more Android submodules are missing or not checked out."
    }

    if ($dirty) {
        Write-Host ""
        Write-Host "Warning: working tree contains local changes." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Telegram Android source verification SUCCESSFUL." -ForegroundColor Green
}
finally {
    Pop-Location
}
