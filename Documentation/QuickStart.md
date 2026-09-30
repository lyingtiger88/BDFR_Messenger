# BDFR Messenger Backend Quick Start

## Requirements
- .NET 9 SDK
- Docker Desktop (or Docker Engine with Compose)

## 1. Start PostgreSQL and Redis
From the repository root:

```bash
docker compose -f Infrastructure/docker-compose.yml up -d
```

The development database defaults are intentionally local-only. Set `POSTGRES_PASSWORD` before using a shared environment.

## 2. Run the API

```bash
dotnet run --project Backend/BDFR.Gateway/BDFR.Gateway.csproj
```

In Development, the API creates the initial schema automatically.

## 3. Health check

```http
GET /health
```

## 4. Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "alice",
  "email": "alice@example.com",
  "password": "a-long-development-password",
  "deviceName": "Desktop",
  "platform": "Windows"
}
```

The response contains a short-lived access token and a rotating refresh token.

## 5. Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "login": "alice@example.com",
  "password": "a-long-development-password",
  "deviceName": "Desktop",
  "platform": "Windows"
}
```

`login` accepts either the normalized username or email address.

## 6. Refresh

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "<refresh token>"
}
```

Refresh tokens are stored server-side only as SHA-256 hashes and are rotated on refresh.

## 7. Logout

```http
POST /api/auth/logout
Content-Type: application/json

{
  "refreshToken": "<refresh token>"
}
```

## Production notes
Do not use the development keys in `appsettings.json`. Override the JWT signing key, the AES-256 data key, PostgreSQL password, and connection strings using a secret manager or protected environment variables.
