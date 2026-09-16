using Microsoft.EntityFrameworkCore;
namespace Talume.Web.Data;
public static class SchemaUpdates
{
    // Additive upgrade from v1 for local development. Production uses the reviewed SQL file.
    // Supported by PostgreSQL and the development SQLite test provider.
    public static Task ApplyLocalAsync(AppDbContext db) => db.Database.ExecuteSqlRawAsync("""
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
""");
}
