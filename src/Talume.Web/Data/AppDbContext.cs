using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Talume.Web.Models;
namespace Talume.Web.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<BusinessLogo> BusinessLogos => Set<BusinessLogo>();
    public DbSet<QuoteLogo> QuoteLogos => Set<QuoteLogo>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<CatalogService> CatalogServices => Set<CatalogService>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<ProjectUpdate> ProjectUpdates => Set<ProjectUpdate>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<EmailChallenge> EmailChallenges => Set<EmailChallenge>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<BusinessLogo>().HasKey(x => x.OwnerId);
        b.Entity<BusinessLogo>().HasOne<AppUser>().WithOne().HasForeignKey<BusinessLogo>(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<QuoteLogo>().HasKey(x => x.QuoteId);
        b.Entity<QuoteLogo>().HasOne<Quote>().WithOne().HasForeignKey<QuoteLogo>(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<BusinessProfile>().HasKey(x => x.OwnerId);
        b.Entity<BusinessProfile>().HasOne<AppUser>().WithOne().HasForeignKey<BusinessProfile>(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Client>().HasIndex(x => new { x.OwnerId, x.Email }).IsUnique();
        b.Entity<Client>().HasIndex(x => x.UserId);
        b.Entity<Client>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Client>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Project>().HasIndex(x => x.QuoteId).IsUnique();
        b.Entity<Project>().HasOne<Quote>().WithMany().HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Quote>().Ignore(x => x.Total);
        b.Entity<Quote>().HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Project>().HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Invitation>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<Invitation>().HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<EmailChallenge>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var p in entity.GetProperties())
                if (p.ClrType == typeof(decimal)) { p.SetPrecision(14); p.SetScale(2); }
    }
}
