# BDFR Messenger API Contract v1

The Desktop and Android clients must use the same BDFR backend contract.

## Identity
- Registration supports email, username and password.
- Login supports email or username plus password.
- Passwords are never stored in plaintext.
- Sessions are issued by BDFR Gateway.

## Core resources
- User
- Session
- Conversation
- Message
- Attachment

## Client rule
Desktop and Android must not implement separate authentication or messaging semantics. Client-specific UI/adapters call the same backend endpoints and map responses into their native Telegram-derived models.

## Compatibility
Any breaking API change requires a new contract version.
