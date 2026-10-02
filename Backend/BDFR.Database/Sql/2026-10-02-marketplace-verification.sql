-- BDFR Marketplace / seller verification schema upgrade.
-- Run against an existing PostgreSQL database before enabling seller verification.
-- The development server uses EnsureCreated for fresh databases.

ALTER TABLE "Users"
    ADD COLUMN IF NOT EXISTS "IsSellerVerified" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "SellerVerifiedAt" timestamptz NULL,
    ADD COLUMN IF NOT EXISTS "NationalCodeEncrypted" varchar(512) NULL,
    ADD COLUMN IF NOT EXISTS "BirthDateEncrypted" varchar(512) NULL;

CREATE INDEX IF NOT EXISTS "IX_Users_IsSellerVerified"
    ON "Users" ("IsSellerVerified");

CREATE TABLE IF NOT EXISTS "MarketplaceStores" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "OwnerUserId" uuid NOT NULL,
    "Name" varchar(120) NOT NULL,
    "Description" varchar(2000) NULL,
    "Kind" integer NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    CONSTRAINT "FK_MarketplaceStores_Users_OwnerUserId"
        FOREIGN KEY ("OwnerUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_MarketplaceStores_OwnerUserId"
    ON "MarketplaceStores" ("OwnerUserId");

CREATE TABLE IF NOT EXISTS "SellerBankVerifications" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "StoreId" uuid NOT NULL,
    "CardNumberEncrypted" varchar(512) NULL,
    "IbanEncrypted" varchar(512) NULL,
    "AccountNumberEncrypted" varchar(512) NULL,
    "BankCode" varchar(8) NULL,
    "CardLast4" varchar(4) NULL,
    "BankName" varchar(120) NULL,
    "Status" integer NOT NULL,
    "Provider" varchar(120) NULL,
    "VerifiedAt" timestamptz NULL,
    "CreatedAt" timestamptz NOT NULL,
    CONSTRAINT "FK_SellerBankVerifications_MarketplaceStores_StoreId"
        FOREIGN KEY ("StoreId") REFERENCES "MarketplaceStores" ("Id") ON DELETE CASCADE
);

ALTER TABLE "SellerBankVerifications"
    ADD COLUMN IF NOT EXISTS "AccountNumberEncrypted" varchar(512) NULL,
    ADD COLUMN IF NOT EXISTS "BankCode" varchar(8) NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_SellerBankVerifications_StoreId"
    ON "SellerBankVerifications" ("StoreId");
