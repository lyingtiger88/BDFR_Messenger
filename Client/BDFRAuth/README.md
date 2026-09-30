# BDFRAuth adapter

This directory is reserved for the Telegram Desktop-side adapter that communicates with the private BDFR backend.

Initial backend contract:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/users/search?q=...`
- `POST /api/messages/to/{recipientId}`
- `GET /api/messages/with/{userId}`
- SignalR: `/hubs/chat`

The adapter must never persist plaintext passwords. Refresh tokens should be placed in the OS credential store rather than ordinary Telegram local settings.
