# BDFR Messenger Architecture

## Direction
- Telegram client source is the UI/client foundation.
- BDFR owns the backend API and account system.
- Authentication is email/username + password rather than phone/SMS.
- Passwords are hashed with Argon2id.
- Sensitive application data uses authenticated encryption (AES-256-GCM).
- PostgreSQL is the primary database; Redis is reserved for sessions, cache and realtime state.

## Planned services
1. API Gateway
2. Authentication
3. Security / key management
4. Messaging
5. Media
6. Notifications

## Security baseline
TLS is mandatory in deployment. Keys and production secrets must never be committed to Git.
