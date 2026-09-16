using Microsoft.AspNetCore.Identity;
namespace Talume.Web.Models;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public string Kind { get; set; } = "Freelancer";
}
// OwnerId is the freelancer's Identity ID. Every query must enforce this boundary.
public class Client
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public string? UserId { get; set; }
    public string Name { get; set; } = "";
    public string Company { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Notes { get; set; } = "";
}
public class CatalogService
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal BasePrice { get; set; }
    public int EstimatedDays { get; set; }
}
public class Quote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Rascunho";
    public DateOnly ValidUntil { get; set; }
    public decimal Discount { get; set; }
    public string Terms { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<QuoteLine> Lines { get; set; } = [];
    public decimal Total => Math.Round(Lines.Sum(l => l.Quantity * l.UnitPrice) - Discount, 2);
}
public class QuoteLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuoteId { get; set; }
    // Snapshot: changing the catalog never changes a negotiated quote.
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid? QuoteId { get; set; }
    public string Title { get; set; } = "";
    public string Stage { get; set; } = "Briefing";
    public DateOnly DueDate { get; set; }
    public decimal Value { get; set; }
    public string ClientPending { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ProjectTask> Tasks { get; set; } = [];
    public List<ProjectUpdate> Updates { get; set; } = [];
    public List<Delivery> Deliveries { get; set; } = [];
    public List<Installment> Installments { get; set; } = [];
}
public class ProjectTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = "";
    public DateOnly DueDate { get; set; }
    public bool Done { get; set; }
}
public class ProjectUpdate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Text { get; set; } = "";
    public bool Internal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Delivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Installment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Label { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
}
public class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
public class EmailChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string Purpose { get; set; } = "confirm";
    public string CodeHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? InvitationId { get; set; }
}

// One issuer profile per freelancer. Clients only see it on authorized proposals.
public class BusinessProfile
{
    public string OwnerId { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string ResponsibleName { get; set; } = "";
    public string TaxId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Website { get; set; } = "";
    public string Address { get; set; } = "";
}

public class BusinessLogo
{
    public string OwnerId { get; set; } = "";
    public string DataUrl { get; set; } = "";
}
public class QuoteLogo
{
    public Guid QuoteId { get; set; }
    public string DataUrl { get; set; } = "";
}
