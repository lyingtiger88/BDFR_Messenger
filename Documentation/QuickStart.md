# BDFR Messenger Backend Quick Start

## Fastest path (Windows / Linux / macOS)

From the repository root:

```bash
docker compose -f Infrastructure/docker-compose.yml up --build -d
```

The API is exposed at:

```text
http://localhost:8080
```

Check it:

```text
GET http://localhost:8080/health
```

## Windows smoke test

After Docker Compose is healthy:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\Test-MessengerMvp.ps1
```

The script creates two temporary users, searches for the second user, sends a private message, and reads the conversation back from the second account.

## Authentication API

### Register

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

### Login

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

### Refresh / logout

```text
POST /api/auth/refresh
POST /api/auth/logout
```

Refresh tokens are random 64-byte values. Only SHA-256 hashes of them are kept in the database, and refresh rotates the token.

## Messaging API

Use the access token as:

```text
Authorization: Bearer <accessToken>
```

Search for a user:

```text
GET /api/users/search?q=bob
```

Send a private message:

```http
POST /api/messages/to/<recipient-user-id>
Content-Type: application/json

{
  "content": "Hello from BDFR Messenger"
}
```

Load recent conversation history:

```text
GET /api/messages/with/<other-user-id>?take=50
```

Mark a received message read:

```text
POST /api/messages/<message-id>/read
```

## Realtime

Authenticated SignalR connections use:

```text
/hubs/chat
```

The current realtime client events are:

- `messageReceived`
- `messageSent`
- `typing`

Hub methods:

- `SendDirectMessage(recipientId, content)`
- `Typing(recipientId, isTyping)`

## Security currently implemented

- Argon2id password hashing
- AES-256-GCM for encrypted email storage
- separate SHA-256 email lookup index
- 15-minute signed JWT access tokens
- rotating 30-day refresh sessions
- refresh tokens hashed at rest
- per-IP API rate limiting
- authenticated SignalR
- PostgreSQL persistence
- production secrets excluded from source control

## Important

The values in the default development configuration are intentionally non-production secrets. Before any public deployment, override:

- `POSTGRES_PASSWORD`
- `JWT_SIGNING_KEY`
- `DATA_ENCRYPTION_KEY_BASE64`

Use a real secret manager in production.
