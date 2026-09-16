-- Additive upgrade. Apply after 002; back up the database first.
CREATE TABLE IF NOT EXISTS "BusinessLogos" (
    "OwnerId" text NOT NULL,
    "DataUrl" text NOT NULL,
    CONSTRAINT "PK_BusinessLogos" PRIMARY KEY ("OwnerId"),
    CONSTRAINT "FK_BusinessLogos_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "QuoteLogos" (
    "QuoteId" uuid NOT NULL,
    "DataUrl" text NOT NULL,
    CONSTRAINT "PK_QuoteLogos" PRIMARY KEY ("QuoteId"),
    CONSTRAINT "FK_QuoteLogos_Quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES "Quotes" ("Id") ON DELETE CASCADE
);
