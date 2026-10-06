param([switch]$SkipDockerStart)

$ErrorActionPreference = "Stop"
$base = "http://localhost:8080"
$repoRoot = Split-Path -Parent $PSScriptRoot
$compose = Join-Path $repoRoot "Infrastructure\docker-compose.yml"

function Post-Json($url, $body, $token = $null) {
    $headers = @{}
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    Invoke-RestMethod -Method Post -Uri $url -ContentType "application/json" -Headers $headers -Body ($body | ConvertTo-Json)
}

if (-not $SkipDockerStart) {
    Write-Host "Starting BDFR Messenger backend..."
    docker compose -f "$compose" up --build -d
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose/build failed. Review the Docker output above."
    }
}

Write-Host "Waiting for gateway..."
$ready = $false
for ($i = 0; $i -lt 60; $i++) {
    try {
        $health = Invoke-RestMethod "$base/health" -TimeoutSec 2
        if ($health.status -eq "ok") {
            $ready = $true
            break
        }
    } catch {
    }
    Start-Sleep -Seconds 2
}

if (-not $ready) {
    docker compose -f "$compose" ps
    docker compose -f "$compose" logs --tail 80 gateway
    throw "Gateway did not become ready."
}

Write-Host "Gateway is ready."
Invoke-RestMethod "$base/health" | ConvertTo-Json

$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$aliceName = "alice$stamp"
$bobName = "bob$stamp"

Write-Host "Registering Alice..."
$alice = Post-Json "$base/api/auth/register" @{
    username = $aliceName
    email = "$aliceName@example.com"
    password = "Alice-development-password-123!"
    deviceName = "PowerShell"
    platform = "Windows"
}

Write-Host "Registering Bob..."
$bob = Post-Json "$base/api/auth/register" @{
    username = $bobName
    email = "$bobName@example.com"
    password = "Bob-development-password-123!"
    deviceName = "PowerShell"
    platform = "Windows"
}

Write-Host "Searching for Bob..."
$headers = @{ Authorization = "Bearer $($alice.accessToken)" }
$found = Invoke-RestMethod -Method Get -Uri "$base/api/users/search?q=$bobName" -Headers $headers
$found | ConvertTo-Json

Write-Host "Sending Alice -> Bob..."
$message = Post-Json "$base/api/messages/to/$($bob.userId)" @{
    content = "Hello from BDFR Messenger MVP"
} $alice.accessToken
$message | ConvertTo-Json

Write-Host "Reading conversation as Bob..."
$headers = @{ Authorization = "Bearer $($bob.accessToken)" }
$conversation = Invoke-RestMethod -Method Get -Uri "$base/api/messages/with/$($alice.userId)" -Headers $headers
$conversation | ConvertTo-Json

$received = @($conversation) | Where-Object { $_.id -eq $message.id } | Select-Object -First 1
if (-not $received) {
    throw "Sent message was not returned in Bob's conversation."
}

Write-Host "Marking message as read..."
Invoke-RestMethod -Method Post -Uri "$base/api/messages/$($message.id)/read" -Headers $headers -ContentType "application/json" -Body "{}" | Out-Null

Write-Host "Verifying read receipt..."
$conversationAfterRead = Invoke-RestMethod -Method Get -Uri "$base/api/messages/with/$($alice.userId)" -Headers $headers
$readBack = @($conversationAfterRead) | Where-Object { $_.id -eq $message.id } | Select-Object -First 1
if (-not $readBack -or -not $readBack.readAt) {
    throw "Read receipt was not persisted for message $($message.id)."
}
$readBack | ConvertTo-Json

Write-Host ""
Write-Host "BDFR Messenger backend MVP + read receipt test completed successfully."
