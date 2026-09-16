-- Additive upgrade for the initial Talume schema. Back up your database first.
CREATE TABLE IF NOT EXISTS "BusinessProfiles" (
    "OwnerId" text NOT NULL,
    "BusinessName" text NOT NULL,
    "ResponsibleName" text NOT NULL,
    "TaxId" text NOT NULL,
    "Email" text NOT NULL,
    "Phone" text NOT NULL,
    "Website" text NOT NULL,
    "Address" text NOT NULL,
    CONSTRAINT "PK_BusinessProfiles" PRIMARY KEY ("OwnerId"),
    CONSTRAINT "FK_BusinessProfiles_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);
