# BDFR Messenger

BDFR Messenger is a Telegram Desktop-based messenger project with a private BDFR backend and an independent account system.

## Current usable milestone

The backend MVP currently provides:

- Email / username + password accounts
- Argon2id password hashing
- AES-256-GCM encrypted email storage
- AES-256-GCM encrypted message storage
- JWT access tokens
- Rotating refresh sessions
- PostgreSQL persistence
- Redis development service
- User search
- Private direct messages
- Conversation history
- Read status
- SignalR realtime delivery and typing events
- Docker Compose startup
- Automated backend tests and CI configuration

## Run the MVP

```bash
docker compose -f Infrastructure/docker-compose.yml up --build -d
```

API:

```text
http://localhost:8080
```

On Windows, run the included end-to-end smoke test:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\Test-MessengerMvp.ps1
```

The script creates two users, finds the second user, sends a direct message, and loads the conversation from the other account.

See:

- `Documentation/QuickStart.md`
- `Documentation/Architecture.md`

## Telegram Desktop client foundation

The desktop client is based on the official Telegram Desktop source:

```text
https://github.com/telegramdesktop/tdesktop
```

Pinned upstream commit:

```text
0b4a7faa9d99ba24ab4f3624cd3d7a5d38cc1ef8
```

Bootstrap it on Windows with:

```powershell
.\Client\Bootstrap-TelegramDesktop.ps1
```

The BDFR Qt authentication adapter is under:

```text
Client/BDFRAuth/
```

The upstream phone-login entry points already identified for replacement are documented in:

```text
Client/Patches/README.md
```

## Next development milestone

Replace Telegram Desktop's phone/SMS intro flow with the BDFR email/username + password flow and connect the Telegram chat UI to the BDFR messaging API.

## Security note

Development secrets in the repository are placeholders only. Production deployment must provide independent JWT signing keys, AES data-encryption keys, database credentials, TLS certificates, and a proper secret manager.
