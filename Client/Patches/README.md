# BDFR Telegram Desktop patch layer

Upstream is pinned in `Client/TelegramDesktop.UPSTREAM`.

## Authentication entry points identified in upstream

At the pinned commit, the phone login flow is primarily implemented in:

- `Telegram/SourceFiles/intro/intro_phone.h`
- `Telegram/SourceFiles/intro/intro_phone.cpp`
- `Telegram/SourceFiles/intro/intro_code.h`
- `Telegram/SourceFiles/intro/intro_code.cpp`

The MTProto phone-auth schema is declared in:

- `Telegram/SourceFiles/mtproto/scheme/api.tl`

The current upstream phone widget invokes `MTPauth_SendCode`, stores the returned phone-code hash, and advances to `CodeWidget`.

## BDFR replacement plan

The BDFR client will not call Telegram's `auth.sendCode` for account authentication.

The first BDFR patch will replace the initial login step with:

1. Username/email input.
2. Password input.
3. HTTPS request to `POST /api/auth/login`.
4. Store the returned access token and refresh token in the platform secure store.
5. Establish BDFR realtime connection at `/hubs/chat`.
6. Enter the BDFR conversation shell after successful authentication.

Phone/SMS code widgets remain upstream code until the BDFR auth replacement is complete, then they will be removed from the BDFR navigation path.

## Update policy

Do not make unrelated edits directly throughout upstream. Keep BDFR-specific changes isolated and documented so future Telegram Desktop updates can be rebased with a small patch surface.
