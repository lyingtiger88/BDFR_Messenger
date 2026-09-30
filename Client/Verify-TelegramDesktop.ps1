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

    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        $cppgirOnly = @($bad | Where-Object { $_ -match "cmake/external/glib/cppgir" })
        $bad = @($bad | Where-Object { $_ -notmatch "cmake/external/glib/cppgir" })
        if ($cppgirOnly.Count -gt 0) {
            Write-Host "Ignoring cppgir on Windows (GLib/Linux-only build dependency)." -ForegroundColor Yellow
        }
    }

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
                if ($IsWindows -or $env:OS -eq "Windows_NT") {
                    Write-Host "cppgir is not required for the Windows build and will be skipped." -ForegroundColor Yellow
                } else {
                    throw "cppgir dependency is blocked by GitLab HTTP 403."
                }
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
