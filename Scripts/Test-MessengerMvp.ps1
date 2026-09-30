$ErrorActionPreference = "Stop"
$base = "http://localhost:8080"

function Post-Json($url, $body, $token = $null) {
    $headers = @{}
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    Invoke-RestMethod -Method Post -Uri $url -ContentType "application/json" -Headers $headers -Body ($body | ConvertTo-Json)
}

Write-Host "Checking gateway..."
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

Write-Host ""
Write-Host "BDFR Messenger backend MVP test completed successfully."
