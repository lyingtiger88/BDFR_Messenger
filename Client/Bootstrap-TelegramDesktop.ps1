$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $PSScriptRoot "TelegramDesktop"
$upstream = "https://github.com/telegramdesktop/tdesktop.git"
$commit = "0b4a7faa9d99ba24ab4f3624cd3d7a5d38cc1ef8"

if (Test-Path $target) {
    throw "Target already exists: $target"
}

Write-Host "Cloning Telegram Desktop..."
git clone --recursive $upstream $target

Push-Location $target
try {
    Write-Host "Checking out pinned upstream commit $commit..."
    git checkout $commit
    git submodule update --init --recursive
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Telegram Desktop source is ready at:"
Write-Host $target
Write-Host "Pinned commit: $commit"
