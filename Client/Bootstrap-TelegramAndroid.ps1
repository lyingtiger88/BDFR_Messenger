$ErrorActionPreference = "Stop"

$root = Join-Path $PSScriptRoot "TelegramAndroid"
$url = "https://github.com/DrKLO/Telegram.git"

Write-Host "=========================================="
Write-Host "BDFR Messenger - Telegram Android Bootstrap"
Write-Host "=========================================="
Write-Host ""

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git was not found in PATH."
}

if (Test-Path $root) {
    if (-not (Test-Path (Join-Path $root ".git"))) {
        throw "TelegramAndroid exists but is not a Git repository: $root"
    }

    Write-Host "Telegram Android source already exists."
    Push-Location $root
    try {
        git fetch --depth=1 origin master
        if ($LASTEXITCODE -ne 0) { throw "Git fetch failed." }
        git checkout master
        git reset --hard origin/master
        if ($LASTEXITCODE -ne 0) { throw "Unable to update master." }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "Cloning official Telegram Android source with submodules..."
    git clone --recursive --shallow-submodules --depth=1 $url $root
    if ($LASTEXITCODE -ne 0) { throw "Telegram Android clone failed." }
}

Push-Location $root
try {
    Write-Host "Ensuring nested submodules are initialized..."
    git submodule update --init --recursive --depth=1
    if ($LASTEXITCODE -ne 0) { throw "Telegram Android submodule initialization failed." }

    $commit = (git rev-parse HEAD).Trim()
    if (-not $commit) { throw "Could not determine Telegram Android commit." }

    Write-Host ""
    Write-Host "Telegram Android source prepared successfully." -ForegroundColor Green
    Write-Host "Pinned working commit: $commit"
    Write-Host "Source: $root"
}
finally {
    Pop-Location
}
