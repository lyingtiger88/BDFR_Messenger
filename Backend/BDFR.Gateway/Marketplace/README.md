# BDFR Marketplace seller verification

A user profile can own one marketplace store for products, services, or both. Store creation is restricted to verified sellers/service providers.

## Verification requirements

The seller must provide **exactly one** Iranian banking identifier:

- 16-digit bank card
- 26-character Iranian IBAN
- Bank account number + Iranian bank code

The backend also requires the seller's 10-digit national code. A birth date is required for card verification. For an IBAN or bank account, API.IR's IbanMatchPro flow verifies ownership against the national code.

## Verification flow

1. Client sends store information and one banking identifier.
2. Backend validates the identifier locally.
3. API.IR is called server-side:
   - Card -> CardToIban + CardMatch
   - IBAN -> IbanInfo + IbanMatchPro
   - Account number -> BankAccountInfo + IbanMatchPro
4. The seller is marked verified only after the ownership match returns true.
5. Identity and banking values are encrypted at rest.
6. Public responses expose only verification state, badge type, badge color, and the last four card digits. Raw identity/bank values are never returned.
7. A verified seller is represented by:
   - `IsSellerVerified=true`
   - `VerificationBadge=verified_seller`
   - `VerificationBadgeColor=blue`

The client should render the colored check beside the profile username.

## API

- `POST /api/marketplace/stores` — create a verified store.
- `GET /api/marketplace/stores/me` — authenticated user's store.
- `GET /api/marketplace/stores/{ownerUserId}` — public active store.
- `PUT /api/marketplace/stores/me` — update the store; supplying a new banking identifier re-runs verification.
- `GET /api/users/me` — profile plus seller badge state.
- `GET /api/users/search?q=...` — user search plus store/badge state.

## API.IR

The implementation uses API.IR's documented CardMatch, IbanMatchPro, CardToIban, IbanInfo and BankAccountInfo services. API.IR uses a Bearer token and recommends keeping the token outside source control.

Configure the server with:

`BankValidation__ApiIrToken=YOUR_API_IR_TOKEN`

Optional base URL:

`BankValidation__BaseUrl=https://s.api.ir`

Never put the API token in the Windows client, Android client, appsettings committed to Git, or source code.

## Database

For an existing PostgreSQL database, apply:

`Backend/BDFR.Database/Sql/2026-10-02-marketplace-verification.sql`

Fresh development databases use the existing `EnsureCreated` path.

## Client work

The backend now exposes everything required for the Windows and Android clients. The client should show the blue verification check when `IsSellerVerified` is true and provide a profile action for creating/editing the store.

