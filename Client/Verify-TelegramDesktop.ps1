$ErrorActionPreference = "Stop"

$root = Join-Path $PSScriptRoot "TelegramDesktop"
$expected = "0b4a7faa9d99ba24ab4f3624cd3d7a5d38cc1ef8"

if (-not (Test-Path $root)) {
    throw "TelegramDesktop folder was not found: $root"
}

if (-not (Test-Path (Join-Path $root ".git"))) {
    throw "TelegramDesktop exists but is not a Git repository."
}

Push-Location $root
try {
    $current = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read Telegram Desktop Git commit."
    }

    Write-Host "Current commit: $current"
    Write-Host "Expected commit: $expected"

    if ($current -ne $expected) {
        Write-Host "WARNING: Telegram Desktop is not on the pinned BDFR commit." -ForegroundColor Yellow
    } else {
        Write-Host "Pinned commit: OK" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Checking submodules..."
    $status = git submodule status --recursive
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to inspect submodules."
    }

    $bad = @($status | Where-Object { $_ -match "^[+-]" })
    if ($bad.Count -gt 0) {
        Write-Host "Some submodules are missing or not on the expected revision:" -ForegroundColor Yellow
        $bad | ForEach-Object { Write-Host $_ }
        Write-Host ""
        Write-Host "Repairing submodules..."
        $repair = git submodule update --init --recursive 2>&1
        $repair | ForEach-Object { Write-Host $_ }

        if ($LASTEXITCODE -ne 0) {
            $repairText = ($repair | Out-String)
            if ($repairText -match "cppgir" -and $repairText -match "403") {
                Write-Host ""
                Write-Host "cppgir could not be cloned from GitLab because GitLab returned HTTP 403." -ForegroundColor Yellow
                Write-Host "The main Telegram Desktop source is present; only this nested dependency is blocked." -ForegroundColor Yellow
                Write-Host "Run Client\Repair-CppGir.bat to repair this dependency separately."
                exit 2
            }
            throw "Submodule repair failed."
        }
    }

    Write-Host ""
    Write-Host "Telegram Desktop source verification SUCCESSFUL." -ForegroundColor Green
}
finally {
    Pop-Location
}
