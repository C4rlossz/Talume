using System.Security.Claims;
using System.Security.Cryptography;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Talume.Web.Data;
using Talume.Web.Models;
using Talume.Web.Services;
namespace Talume.Web.Endpoints;

public static class BusinessEndpoints
{
    public record LogoInput(string? DataUrl);
    public record BusinessInput(string? BusinessName, string? ResponsibleName, string? TaxId, string? Email, string? Phone, string? Website, string? Address);
    public record ClientInput(string Name, string Company, string Email, string Phone, string Notes);
    public record ServiceInput(string Name, string Description, decimal BasePrice, int EstimatedDays);
    public record LineInput(string Description, int Quantity, decimal UnitPrice);
    public record QuoteInput(Guid ClientId, string Title, DateOnly ValidUntil, decimal Discount, string Terms, List<LineInput> Lines, string? LogoDataUrl = null);
    public record ProjectInput(Guid ClientId, string Title, DateOnly DueDate, decimal Value);
    public record ConvertInput(DateOnly DueDate);
    public record ProjectEdit(string Stage, DateOnly DueDate, string ClientPending);
    public record TaskInput(string Title, DateOnly DueDate);
    public record DoneInput(bool Done);
    public record NoteInput(string Text, bool Internal);
    public record DeliveryInput(string Title, string Url);
    public record PaymentInput(string Label, decimal Amount, DateOnly DueDate);
    static IResult Bad(string message) => Results.BadRequest(new { error = message });
    static bool Text(string? s, int max = 160) => !string.IsNullOrWhiteSpace(s) && s.Length <= max;
    static bool Money(decimal value) => value >= 0 && value <= 99999999 && decimal.Round(value, 2) == value;
    static bool Date(DateOnly value) => value.Year is >= 2000 and <= 2200;
    public static void MapBusinessEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> users, IConfiguration config, IWebHostEnvironment env) => {
            var u = await users.GetUserAsync(principal);
            return Results.Ok(new { u!.DisplayName, u.Email, u.Kind, demo = env.IsDevelopment() && config.GetValue("Demo:Seed", false) });
        });
        api.MapGet("/business-logo", async (AppDbContext db, ClaimsPrincipal u) => {
            var logo = await db.BusinessLogos.FindAsync(u.UserId());
            return Results.Ok(new { dataUrl = logo?.DataUrl ?? "" });
        }).RequireAuthorization("Freelancer");
        api.MapPut("/business-logo", async (LogoInput i, AppDbContext db, ClaimsPrincipal u) => {
            if (!LogoImage.Valid(i.DataUrl)) return Bad("A logo precisa ser um PNG válido de até 512 KB e 2000 × 2000 pixels.");
            var logo = await db.BusinessLogos.FindAsync(u.UserId());
            if (logo == null) { logo = new BusinessLogo { OwnerId = u.UserId() }; db.BusinessLogos.Add(logo); }
            logo.DataUrl = i.DataUrl ?? ""; await db.SaveChangesAsync(); return Results.Ok(new { logo.DataUrl });
        }).RequireAuthorization("Freelancer");
        api.MapGet("/business-profile", async (AppDbContext db, ClaimsPrincipal u) => {
            var profile = await db.BusinessProfiles.FindAsync(u.UserId());
            if (profile == null) {
                var user = await db.Users.FindAsync(u.UserId());
                profile = new BusinessProfile { OwnerId = u.UserId(), BusinessName = user!.DisplayName, ResponsibleName = user.DisplayName };
            }
            return Results.Ok(profile);
        }).RequireAuthorization("Freelancer");
        api.MapPut("/business-profile", async (BusinessInput i, AppDbContext db, ClaimsPrincipal u) => {
            var name = (i.BusinessName ?? "").Trim(); var responsible = (i.ResponsibleName ?? "").Trim();
            var taxId = (i.TaxId ?? "").Trim(); var email = (i.Email ?? "").Trim();
            var phone = (i.Phone ?? "").Trim(); var website = (i.Website ?? "").Trim(); var address = (i.Address ?? "").Trim();
            if (!Text(name) || responsible.Length > 160 || taxId.Length > 30 || email.Length > 254 || phone.Length > 30 || website.Length > 300 || address.Length > 400)
                return Bad("Informe o nome da empresa e respeite os limites dos campos.");
            if (email.Length > 0 && !System.Net.Mail.MailAddress.TryCreate(email, out _)) return Bad("Confira o e-mail comercial.");
            if (website.Length > 0 && (!Uri.TryCreate(website, UriKind.Absolute, out var url) || url.Scheme != "https" || string.IsNullOrEmpty(url.Host))) return Bad("Informe o site completo, começando com https://.");
            var profile = await db.BusinessProfiles.FindAsync(u.UserId());
            if (profile == null) { profile = new BusinessProfile { OwnerId = u.UserId() }; db.BusinessProfiles.Add(profile); }
            profile.BusinessName = name; profile.ResponsibleName = responsible; profile.TaxId = taxId;
            profile.Email = email; profile.Phone = phone; profile.Website = website; profile.Address = address;
            await db.SaveChangesAsync(); return Results.Ok(profile);
        }).RequireAuthorization("Freelancer");
        api.MapGet("/clients", async (AppDbContext db, ClaimsPrincipal u) => await db.Clients.Where(x => x.OwnerId == u.UserId()).OrderBy(x => x.Name).ToListAsync()).RequireAuthorization("Freelancer");
        api.MapPost("/clients", async (ClientInput i, AppDbContext db, ClaimsPrincipal u) => {
            var email = i.Email.Trim().ToLowerInvariant();
            if (!Text(i.Name) || !System.Net.Mail.MailAddress.TryCreate(email, out _) || email.Length > 254 || i.Notes.Length > 5000 || i.Phone.Length > 30 || i.Company.Length > 160) return Bad("Confira nome, e-mail e os limites dos campos.");
            if (await db.Clients.AnyAsync(x => x.OwnerId == u.UserId() && x.Email == email)) return Bad("Você já cadastrou um cliente com este e-mail.");
            var c = new Client { OwnerId = u.UserId(), Name = i.Name.Trim(), Company = i.Company.Trim(), Email = email, Phone = i.Phone.Trim(), Notes = i.Notes };
            db.Clients.Add(c); await db.SaveChangesAsync(); return Results.Ok(c);
        }).RequireAuthorization("Freelancer");
        api.MapPut("/clients/{id:guid}", async (Guid id, ClientInput i, AppDbContext db, ClaimsPrincipal u) => {
            var c = await db.Clients.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.UserId());
            if (c == null) return Results.NotFound();
            var email = i.Email.Trim().ToLowerInvariant();
            if (!Text(i.Name) || !System.Net.Mail.MailAddress.TryCreate(email, out _) || email.Length > 254 || i.Notes.Length > 5000 || i.Phone.Length > 30 || i.Company.Length > 160) return Bad("Confira os campos do cliente.");
            if (c.UserId != null && c.Email != email) return Bad("O e-mail de uma conta vinculada não pode ser alterado por aqui.");
            if (await db.Clients.AnyAsync(x => x.OwnerId == u.UserId() && x.Email == email && x.Id != id)) return Bad("Outro cliente já usa este e-mail.");
            if (c.Email != email) {
                foreach (var inv in await db.Invitations.Where(x => x.ClientId == id && x.UsedAt == null).ToListAsync()) inv.ExpiresAt = DateTime.UtcNow;
            }
            c.Name = i.Name.Trim(); c.Company = i.Company.Trim(); c.Email = email; c.Phone = i.Phone.Trim(); c.Notes = i.Notes;
            await db.SaveChangesAsync(); return Results.Ok(c);
        }).RequireAuthorization("Freelancer");
        api.MapPost("/clients/{id:guid}/invite", async (Guid id, AppDbContext db, ClaimsPrincipal u, MailSender mail, IConfiguration config) => {
            var c = await db.Clients.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.UserId());
            if (c == null) return Results.NotFound();
            if (c.UserId != null) return Bad("Este cliente já tem acesso ao portal.");
            if (await db.Invitations.AnyAsync(x => x.ClientId == id && x.ExpiresAt > DateTime.UtcNow.AddDays(7).AddMinutes(-1))) return Bad("Aguarde um minuto para reenviar o convite.");
            var previous = await db.Invitations.Where(x => x.ClientId == id && x.UsedAt == null).ToListAsync();
            foreach (var old in previous) old.ExpiresAt = DateTime.UtcNow;
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var invitation = new Invitation { ClientId = id, TokenHash = AuthEndpoints.Hash(token), ExpiresAt = DateTime.UtcNow.AddDays(7) };
            db.Invitations.Add(invitation); await db.SaveChangesAsync();
            var url = (config["App:BaseUrl"] ?? "http://localhost:8080").TrimEnd('/') + "/Account?invite=" + token;
            try { await mail.SendInvitationAsync(c.Email, c.Name, url); }
            catch (MailDeliveryException) { invitation.ExpiresAt = DateTime.UtcNow; await db.SaveChangesAsync(); return Results.Json(new { error = "Falha no envio. Confira o serviço de e-mail e tente novamente." }, statusCode: 503); }
            return Results.Ok(new { message = "Convite enviado por e-mail." });
        }).RequireAuthorization("Freelancer");
        api.MapGet("/services", async (AppDbContext db, ClaimsPrincipal u) => await db.CatalogServices.Where(x => x.OwnerId == u.UserId()).OrderBy(x => x.Name).ToListAsync()).RequireAuthorization("Freelancer");
        api.MapPost("/services", async (ServiceInput i, AppDbContext db, ClaimsPrincipal u) => {
            if (!Text(i.Name) || i.Description.Length > 3000 || !Money(i.BasePrice) || i.EstimatedDays is < 1 or > 365) return Bad("Confira nome, preço e prazo (1 a 365 dias).");
            var service = new CatalogService { OwnerId = u.UserId(), Name = i.Name.Trim(), Description = i.Description, BasePrice = i.BasePrice, EstimatedDays = i.EstimatedDays };
            db.CatalogServices.Add(service); await db.SaveChangesAsync(); return Results.Ok(service);
        }).RequireAuthorization("Freelancer");
        api.MapPut("/services/{id:guid}", async (Guid id, ServiceInput i, AppDbContext db, ClaimsPrincipal u) => {
            var s = await db.CatalogServices.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.UserId());
            if (s == null) return Results.NotFound();
            if (!Text(i.Name) || i.Description.Length > 3000 || !Money(i.BasePrice) || i.EstimatedDays is < 1 or > 365) return Bad("Confira nome, preço e prazo.");
            s.Name = i.Name.Trim(); s.Description = i.Description; s.BasePrice = i.BasePrice; s.EstimatedDays = i.EstimatedDays;
            await db.SaveChangesAsync(); return Results.Ok(s);
        }).RequireAuthorization("Freelancer");
        api.MapGet("/quotes", async (AppDbContext db, ClaimsPrincipal u) => {
            var quotes = await db.VisibleQuotes(u).Include(x => x.Client).Include(x => x.Lines).OrderByDescending(x => x.CreatedAt).ToListAsync();
            return quotes.Select(q => new { q.Id, q.ClientId, clientName = q.Client.Name, q.Title, q.Status, q.ValidUntil, q.Discount, q.Terms, q.Total, q.Lines });
        });
        api.MapPost("/quotes", async (QuoteInput i, AppDbContext db, ClaimsPrincipal u) => {
            if (!Text(i.Title) || !Date(i.ValidUntil) || i.Terms.Length > 5000 || i.Lines == null || i.Lines.Count is < 1 or > 30 || i.Lines.Any(l => !Text(l.Description, 300) || l.Quantity is < 1 or > 1000 || !Money(l.UnitPrice))) return Bad("Preencha o título e adicione de 1 a 30 itens válidos.");
            if (!LogoImage.Valid(i.LogoDataUrl)) return Bad("Confira a logo: PNG de até 512 KB e 2000 × 2000 pixels.");
            var subtotal = i.Lines.Sum(x => x.Quantity * x.UnitPrice);
            if (!Money(i.Discount) || i.Discount > subtotal || subtotal > 99999999) return Bad("O desconto não pode superar o subtotal. Limite total: R$ 99.999.999.");
            if (!await db.Clients.AnyAsync(x => x.Id == i.ClientId && x.OwnerId == u.UserId())) return Results.NotFound();
            var q = new Quote { OwnerId = u.UserId(), ClientId = i.ClientId, Title = i.Title.Trim(), ValidUntil = i.ValidUntil, Discount = i.Discount, Terms = i.Terms,
                Lines = i.Lines.Select(l => new QuoteLine { Description = l.Description.Trim(), Quantity = l.Quantity, UnitPrice = l.UnitPrice }).ToList() };
            // Null uses the current business logo; an empty string explicitly means no logo.
            var logo = i.LogoDataUrl ?? (await db.BusinessLogos.FindAsync(u.UserId()))?.DataUrl ?? "";
            db.Quotes.Add(q); db.QuoteLogos.Add(new QuoteLogo { QuoteId = q.Id, DataUrl = logo });
            await db.SaveChangesAsync(); return Results.Ok(new { q.Id });
        }).RequireAuthorization("Freelancer");
        api.MapPost("/quotes/{id:guid}/share", async (Guid id, AppDbContext db, ClaimsPrincipal u) => {
            var q = await db.Quotes.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.UserId());
            if (q == null) return Results.NotFound();
            if (q.Status == "Rascunho") q.Status = "Enviado";
            await db.SaveChangesAsync(); return Results.Ok();
        }).RequireAuthorization("Freelancer");
        api.MapPost("/quotes/{id:guid}/convert", async (Guid id, ConvertInput input, AppDbContext db, ClaimsPrincipal u) => {
            if (!Date(input.DueDate)) return Bad("Informe uma data de entrega válida.");
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var q = await db.Quotes.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.UserId());
            if (q == null) return Results.NotFound();
            var existing = await db.Projects.SingleOrDefaultAsync(x => x.QuoteId == id);
            if (existing != null) return Results.Ok(new { existing.Id });
            var p = new Project { OwnerId = u.UserId(), ClientId = q.ClientId, QuoteId = q.Id, Title = q.Title, Value = q.Total, DueDate = input.DueDate };
            p.Updates.Add(new ProjectUpdate { Text = "Projeto iniciado após aprovação do orçamento." });
            db.Projects.Add(p); q.Status = "Aprovado";
            await db.SaveChangesAsync(); await transaction.CommitAsync(); return Results.Ok(new { p.Id });
        }).RequireAuthorization("Freelancer");
        api.MapGet("/projects", async (AppDbContext db, ClaimsPrincipal u) => {
            var list = await db.VisibleProjects(u).Include(x => x.Client).Include(x => x.Tasks).Include(x => x.Installments).OrderBy(x => x.DueDate).ToListAsync();
            return list.Select(p => new { p.Id, p.Title, p.Stage, p.DueDate, p.Value, clientName = p.Client.Name, p.ClientPending,
                taskCount = u.Freelancer() ? p.Tasks.Count : (int?)null, doneCount = u.Freelancer() ? p.Tasks.Count(t => t.Done) : (int?)null,
                paid = p.Installments.Where(x => x.PaidAt != null).Sum(x => x.Amount) });
        });
        api.MapPost("/projects", async (ProjectInput i, AppDbContext db, ClaimsPrincipal u) => {
            if (!Text(i.Title) || !Date(i.DueDate) || !Money(i.Value)) return Bad("Confira título, prazo e valor.");
            if (!await db.Clients.AnyAsync(x => x.Id == i.ClientId && x.OwnerId == u.UserId())) return Results.NotFound();
            var p = new Project { OwnerId = u.UserId(), ClientId = i.ClientId, Title = i.Title.Trim(), DueDate = i.DueDate, Value = i.Value };
            p.Updates.Add(new ProjectUpdate { Text = "Projeto iniciado." });
            db.Projects.Add(p); await db.SaveChangesAsync(); return Results.Ok(new { p.Id });
        }).RequireAuthorization("Freelancer");
        api.MapGet("/projects/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal u) => {
            var p = await db.VisibleProjects(u).Include(x => x.Client).Include(x => x.Tasks).Include(x => x.Updates).Include(x => x.Deliveries).Include(x => x.Installments).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id);
            if (p == null) return Results.NotFound();
            return Results.Ok(new { p.Id, p.Title, p.Stage, p.DueDate, p.Value, p.ClientPending, p.QuoteId, clientName = p.Client.Name,
                phone = u.Freelancer() ? p.Client.Phone : null,
                tasks = u.Freelancer() ? p.Tasks.OrderBy(x => x.DueDate).ToList() : [],
                updates = p.Updates.Where(x => u.Freelancer() || !x.Internal).OrderByDescending(x => x.CreatedAt),
                deliveries = p.Deliveries.OrderByDescending(x => x.CreatedAt), installments = p.Installments.OrderBy(x => x.DueDate) });
        });
        api.MapPut("/projects/{id:guid}", async (Guid id, ProjectEdit i, AppDbContext db, ClaimsPrincipal u) => {
            if (!Access.Stages.Contains(i.Stage) || !Date(i.DueDate) || i.ClientPending.Length > 2000) return Bad("Confira a etapa, data e pendência.");
            var p = await db.Projects.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.UserId()); if (p == null) return Results.NotFound();
            var changes = new List<string>();
            if (p.Stage != i.Stage) changes.Add($"Etapa alterada: {p.Stage} → {i.Stage}.");
            if (p.DueDate != i.DueDate) changes.Add($"Entrega prevista atualizada para {i.DueDate:dd/MM/yyyy}.");
            if (p.ClientPending != i.ClientPending) changes.Add(string.IsNullOrWhiteSpace(i.ClientPending) ? "Pendência do cliente concluída." : $"Aguardando cliente: {i.ClientPending}");
            p.Stage = i.Stage; p.DueDate = i.DueDate; p.ClientPending = i.ClientPending;
            if (changes.Count > 0) db.ProjectUpdates.Add(new ProjectUpdate { ProjectId = id, Text = string.Join(" ", changes) });
            await db.SaveChangesAsync(); return Results.Ok();
        }).RequireAuthorization("Freelancer");
        var manage = api.MapGroup("/projects/{id:guid}").RequireAuthorization("Freelancer");
        manage.AddEndpointFilter(async (context, next) => {
            var id = Guid.Parse(context.HttpContext.Request.RouteValues["id"]!.ToString()!);
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            return await db.Projects.AnyAsync(x => x.Id == id && x.OwnerId == context.HttpContext.User.UserId()) ? await next(context) : Results.NotFound();
        });
        manage.MapPost("/tasks", async (Guid id, TaskInput i, AppDbContext db) => {
            if (!Text(i.Title, 300) || !Date(i.DueDate)) return Bad("Informe tarefa e prazo válidos.");
            var t = new ProjectTask { ProjectId = id, Title = i.Title, DueDate = i.DueDate }; db.ProjectTasks.Add(t); await db.SaveChangesAsync(); return Results.Ok(t);
        });
        manage.MapPut("/tasks/{taskId:guid}", async (Guid id, Guid taskId, DoneInput i, AppDbContext db) => {
            var t = await db.ProjectTasks.SingleOrDefaultAsync(x => x.Id == taskId && x.ProjectId == id); if (t == null) return Results.NotFound();
            t.Done = i.Done; await db.SaveChangesAsync(); return Results.Ok();
        });
        manage.MapPost("/updates", async (Guid id, NoteInput i, AppDbContext db) => {
            if (!Text(i.Text, 5000)) return Bad("Escreva uma atualização de até 5.000 caracteres.");
            db.ProjectUpdates.Add(new ProjectUpdate { ProjectId = id, Text = i.Text, Internal = i.Internal }); await db.SaveChangesAsync(); return Results.Ok();
        });
        manage.MapPost("/deliveries", async (Guid id, DeliveryInput i, AppDbContext db) => {
            if (!Text(i.Title) || i.Url.Length > 2000 || !Access.HttpsUrl(i.Url)) return Bad("Informe título e link HTTPS válido.");
            db.Deliveries.Add(new Delivery { ProjectId = id, Title = i.Title, Url = i.Url });
            db.ProjectUpdates.Add(new ProjectUpdate { ProjectId = id, Text = $"Entrega disponível: {i.Title}." }); await db.SaveChangesAsync(); return Results.Ok();
        });
        manage.MapPost("/installments", async (Guid id, PaymentInput i, AppDbContext db) => {
            if (!Text(i.Label) || !Money(i.Amount) || i.Amount == 0 || !Date(i.DueDate)) return Bad("Informe descrição, valor positivo e vencimento.");
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var p = await db.Projects.Include(x => x.Installments).SingleAsync(x => x.Id == id);
            if (p.Installments.Sum(x => x.Amount) + i.Amount > p.Value) return Bad("A soma das parcelas não pode superar o valor do projeto.");
            db.Installments.Add(new Installment { ProjectId = id, Label = i.Label, Amount = i.Amount, DueDate = i.DueDate });
            await db.SaveChangesAsync(); await transaction.CommitAsync(); return Results.Ok();
        });
        manage.MapPost("/installments/{paymentId:guid}/paid", async (Guid id, Guid paymentId, AppDbContext db) => {
            var p = await db.Installments.SingleOrDefaultAsync(x => x.Id == paymentId && x.ProjectId == id); if (p == null) return Results.NotFound();
            p.PaidAt ??= DateTime.UtcNow; await db.SaveChangesAsync(); return Results.Ok();
        });
    }
}
