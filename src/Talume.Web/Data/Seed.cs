using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Talume.Web.Models;
namespace Talume.Web.Data;
public static class Seed
{
    public static async Task RunAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var existing = await users.FindByEmailAsync("freelancer@talume.local");
        if (existing != null) {
            if (!await db.BusinessProfiles.AnyAsync(x => x.OwnerId == existing.Id)) {
                db.BusinessProfiles.Add(new BusinessProfile { OwnerId = existing.Id, BusinessName = "Origins", ResponsibleName = existing.DisplayName });
                await db.SaveChangesAsync();
            }
            return;
        }
        async Task<AppUser> User(string email, string name, string kind) {
            var u = new AppUser { UserName = email, Email = email, DisplayName = name, Kind = kind, EmailConfirmed = true };
            var r = await users.CreateAsync(u, "TalumeDemo2026!"); if (!r.Succeeded) throw new InvalidOperationException(string.Join(", ", r.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(u, kind); return u;
        }
        var owner = await User("freelancer@talume.local", "Carlos", "Freelancer");
        db.BusinessProfiles.Add(new BusinessProfile { OwnerId = owner.Id, BusinessName = "Origins", ResponsibleName = owner.DisplayName });
        var viewer = await User("cliente@talume.local", "Marina Costa", "Client");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var clients = new[] {
            new Client { OwnerId = owner.Id, UserId = viewer.Id, Name = "Marina Costa", Company = "Estúdio Aurora", Email = viewer.Email!, Notes = "Prefere contato no período da tarde." },
            new Client { OwnerId = owner.Id, Name = "Rafael Lima", Company = "Café Horizonte", Email = "rafael@example.com" },
            new Client { OwnerId = owner.Id, Name = "Beatriz Santos", Company = "Forma Arquitetura", Email = "beatriz@example.com" }
        };
        db.Clients.AddRange(clients);
        db.CatalogServices.AddRange(
            new CatalogService { OwnerId = owner.Id, Name = "Site institucional", Description = "Até 5 páginas, layout responsivo e formulário de contato.", BasePrice = 2400, EstimatedDays = 15 },
            new CatalogService { OwnerId = owner.Id, Name = "Identidade visual", Description = "Logo, paleta e guia de aplicação.", BasePrice = 1800, EstimatedDays = 10 },
            new CatalogService { OwnerId = owner.Id, Name = "Conteúdo para redes sociais", Description = "Pacote com 12 peças para o feed.", BasePrice = 900, EstimatedDays = 7 });
        var p1 = new Project { OwnerId = owner.Id, ClientId = clients[0].Id, Title = "Um novo site para o Estúdio Aurora", Stage = "Em produção", DueDate = today.AddDays(5), Value = 2400, ClientPending = "Enviar fotos da equipe para a página Sobre." };
        p1.Tasks.AddRange([
            new ProjectTask { Title = "Organizar briefing e referências", DueDate = today.AddDays(-3), Done = true },
            new ProjectTask { Title = "Finalizar a página inicial", DueDate = today.AddDays(1) },
            new ProjectTask { Title = "Adaptar layout para celular", DueDate = today.AddDays(3) },
            new ProjectTask { Title = "Revisar e preparar entrega", DueDate = today.AddDays(5) }
        ]);
        p1.Updates.AddRange([new ProjectUpdate { Text = "Briefing aprovado. Desenvolvimento iniciado.", CreatedAt = DateTime.UtcNow.AddDays(-2) },new ProjectUpdate { Text = "A estrutura da página inicial está pronta. Próximo passo: inserir as imagens.", CreatedAt = DateTime.UtcNow.AddHours(-2) },new ProjectUpdate { Text = "Reservar duas horas para revisão técnica.", Internal = true }]);
        p1.Installments.AddRange([new Installment { Label = "Entrada · 50%", Amount = 1200, DueDate = today.AddDays(-5), PaidAt = DateTime.UtcNow.AddDays(-5) },new Installment { Label = "Entrega · 50%", Amount = 1200, DueDate = today.AddDays(5) }]);
        var p2 = new Project { OwnerId = owner.Id, ClientId = clients[1].Id, Title = "Identidade visual · Café Horizonte", Stage = "Em revisão", DueDate = today.AddDays(2), Value = 1800 };
        p2.Tasks.Add(new ProjectTask { Title = "Aplicar ajustes na apresentação", DueDate = today, Done = false });
        p2.Updates.Add(new ProjectUpdate { Text = "Primeira versão apresentada. Aguardando revisão." });
        p2.Installments.Add(new Installment { Label = "Pagamento integral", Amount = 1800, DueDate = today.AddDays(2) });
        var p3 = new Project { OwnerId = owner.Id, ClientId = clients[2].Id, Title = "Conteúdo de setembro", Stage = "Briefing", DueDate = today.AddDays(9), Value = 900 };
        p3.Tasks.Add(new ProjectTask { Title = "Alinhar pautas do mês", DueDate = today.AddDays(-1) });
        p3.Updates.Add(new ProjectUpdate { Text = "Projeto iniciado. Vamos organizar as referências." });
        db.Projects.AddRange(p1,p2,p3);
        db.Quotes.Add(new Quote { OwnerId = owner.Id, ClientId = clients[2].Id, Title = "Site · Forma Arquitetura", Status = "Enviado", ValidUntil = today.AddDays(7), Terms = "50% na aprovação e 50% na entrega. Inclui duas rodadas de revisão.", Lines = [new QuoteLine { Description = "Site institucional · até 5 páginas", Quantity = 1, UnitPrice = 2400 }] });
        await db.SaveChangesAsync();
    }
}
