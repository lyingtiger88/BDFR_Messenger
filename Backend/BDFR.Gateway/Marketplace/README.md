# BDFR seller verification

Seller/store creation requires a valid Iranian bank card or IBAN plus identity data.

## Verification flow

1. User submits store data, Iranian national code and birth date.
2. Backend validates the card checksum or Iranian IBAN checksum locally.
3. For a card, API.IR CardToIban identifies the bank/IBAN and CardMatch verifies national-code + birth-date ownership.
4. For an IBAN, API.IR IbanInfo checks that the IBAN is active and IbanMatchPro verifies national-code ownership.
5. Only after the provider confirms the match is the user marked IsSellerVerified=true.
6. Public user responses expose only the boolean badge state; raw identity/bank data is never returned.
7. Card/IBAN/identity values are stored encrypted.

API.IR documents CardMatch and IbanMatchPro as ownership-matching services and uses Bearer API tokens. The server token must never be committed to source control.

## Configuration

Configure the API.IR token through the ASP.NET Core environment/configuration system under:
BankValidation:ApiIrToken

Do not put the token in appsettings.json, the mobile/desktop client, or Git.

## Endpoints

- POST /api/marketplace/stores — create and verify a store.
- GET /api/marketplace/stores/me — read the authenticated user's store.
- PUT /api/marketplace/stores/me — update the store and optionally re-verify bank ownership.

The profile badge is represented by IsSellerVerified and should render next to the username in the Windows and Android clients.

## Database

For an existing PostgreSQL installation, apply:
Backend/BDFR.Database/Sql/2026-10-02-marketplace-verification.sql

Fresh development databases are created by the existing EnsureCreated path.
