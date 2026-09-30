$ErrorActionPreference = "Stop"

$root = Join-Path $PSScriptRoot "TelegramDesktop"
$cmake = Join-Path $root "cmake"

if (-not (Test-Path (Join-Path $root ".git"))) {
    throw "Telegram Desktop source was not found at: $root"
}

Write-Host "Preparing Telegram Desktop submodules for Windows..."

Push-Location $root
try {
    Write-Host "Initializing top-level submodules..."
    git submodule update --init
    if ($LASTEXITCODE -ne 0) {
        throw "Top-level submodule initialization failed."
    }

    if (-not (Test-Path (Join-Path $cmake ".git"))) {
        throw "cmake_helpers submodule is missing."
    }

    Write-Host "Disabling Linux-only cppgir nested submodule for Windows..."
    git -C "$cmake" config submodule.external/glib/cppgir.update none

    Write-Host "Initializing all remaining nested submodules..."
    git submodule update --init --recursive
    if ($LASTEXITCODE -ne 0) {
        throw "Windows submodule initialization failed."
    }

    Write-Host ""
    Write-Host "Windows submodules prepared successfully." -ForegroundColor Green
    Write-Host "cppgir was intentionally skipped because it is a GLib/Linux dependency."
}
finally {
    Pop-Location
}
