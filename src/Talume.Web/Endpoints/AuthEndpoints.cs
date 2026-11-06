using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Talume.Web.Data;
using Talume.Web.Models;
using Talume.Web.Services;
namespace Talume.Web.Endpoints;

public static class AuthEndpoints
{
    public record LoginInput(string Email, string Password);
    public record RegisterInput(string Name, string Email, string Password, string? InvitationToken);
    public record VerifyInput(Guid ChallengeId, string Code, string? NewPassword);
    public record EmailInput(string Email);
    public record InviteInput(string Token);
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    static bool EmailValid(string? value) => value is { Length: > 0 and <= 254 } && MailAddress.TryCreate(value, out var mail) && mail.Address == value;
    static bool InviteValid(string? token) => token is { Length: 64 } && token.All(Uri.IsHexDigit);
    static IResult Error(string message) => Results.BadRequest(new { error = message });

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").RequireRateLimiting("auth");
        auth.MapGet("/config", (IWebHostEnvironment env, IConfiguration config) => Results.Ok(new { demo = env.IsDevelopment() && config.GetValue("Demo:Seed", false) }));
        auth.MapPost("/login", async (LoginInput input, UserManager<AppUser> users, SignInManager<AppUser> signIn) => {
            const string loginError = "Não foi possível entrar. Confira os dados, confirme seu e-mail ou aguarde se houve muitas tentativas.";
            var email = input.Email?.Trim();
            if (!EmailValid(email) || string.IsNullOrEmpty(input.Password)) return Error(loginError);
            var user = await users.FindByEmailAsync(email!);
            if (user == null) return Error(loginError);
            var result = await signIn.PasswordSignInAsync(user, input.Password, false, lockoutOnFailure: true);
            return result.Succeeded ? Results.Ok(new { redirect = "/" }) : Error(loginError);
        });
        auth.MapPost("/logout", async (SignInManager<AppUser> signIn) => { await signIn.SignOutAsync(); return Results.Ok(); }).RequireAuthorization();
        auth.MapPost("/invitation", async (InviteInput input, AppDbContext db) => {
            var token = input.Token;
            if (!InviteValid(token)) return Error("Convite inválido ou expirado. Peça um novo convite ao desenvolvedor.");
            var hash = Hash(token);
            var inv = await db.Invitations.SingleOrDefaultAsync(x => x.TokenHash == hash && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow);
            if (inv == null) return Error("Convite inválido ou expirado. Peça um novo convite ao freelancer.");
            var client = await db.Clients.FindAsync(inv.ClientId);
            return client == null ? Error("Convite inválido ou expirado.") : Results.Ok(new { email = client.Email, name = client.Name });
        });
        auth.MapPost("/register", async (RegisterInput input, AppDbContext db, UserManager<AppUser> users, MailSender mail, EmailCodes codes, IConfiguration config, IWebHostEnvironment env) => {
            var email = (input.Email ?? "").Trim().ToLowerInvariant();
            if (!EmailValid(email) || string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 100 || input.Password is not { Length: >= 10 and <= 256 })
                return Error("Informe nome e e-mail válidos e uma senha de 10 a 256 caracteres.");
            if (string.IsNullOrWhiteSpace(input.InvitationToken) && !RegistrationPolicy.CanRegisterDeveloper(email, config, env))
                return Results.Json(new { error = "O Talume está em piloto. O cadastro de desenvolvedores precisa de autorização." }, statusCode: 403);
            Invitation? inv = null;
            if (!string.IsNullOrWhiteSpace(input.InvitationToken)) {
                if (!InviteValid(input.InvitationToken)) return Error("Convite inválido ou expirado.");
                var hash = Hash(input.InvitationToken);
                inv = await db.Invitations.SingleOrDefaultAsync(x => x.TokenHash == hash && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow);
                if (inv == null) return Error("Convite inválido ou expirado.");
                var client = await db.Clients.FindAsync(inv.ClientId);
                if (client == null || client.Email != email) return Error("Use o e-mail que recebeu o convite.");
            }
            var user = await users.FindByEmailAsync(email);
            if (user != null) {
                // Existing accounts prove mailbox ownership before a new invitation is linked.
                if (inv == null && user.EmailConfirmed) return Error("Se você já possui uma conta, entre ou recupere sua senha.");
            } else {
                user = new AppUser { UserName = email, Email = email, DisplayName = input.Name.Trim(), Kind = inv == null ? "Freelancer" : "Client" };
                var created = await users.CreateAsync(user, input.Password);
                if (!created.Succeeded) return Error("Use uma senha com 10 ou mais caracteres, maiúscula, minúscula, número e símbolo.");
                await users.AddToRoleAsync(user, user.Kind);
            }
            if (inv != null && user.Kind != "Client") return Error("Este e-mail pertence a uma conta de freelancer. Use outro e-mail para o portal do cliente.");
            return await IssueAsync(db, mail, codes, user, "confirm", inv?.Id);
        });
        auth.MapPost("/resend", async (EmailInput input, AppDbContext db, UserManager<AppUser> users, MailSender mail, EmailCodes codes) => {
            var email = input.Email?.Trim();
            var user = EmailValid(email) ? await users.FindByEmailAsync(email!) : null;
            if (user == null || user.EmailConfirmed) return Results.Ok(new { message = "Se houver cadastro pendente, enviaremos um código.", challengeId = Guid.NewGuid() });
            var previous = await db.EmailChallenges.Where(x => x.UserId == user.Id && x.Purpose == "confirm" && x.UsedAt == null).OrderByDescending(x => x.ExpiresAt).FirstOrDefaultAsync();
            return await IssueAsync(db, mail, codes, user, "confirm", previous?.InvitationId);
        });
        auth.MapPost("/forgot", async (EmailInput input, AppDbContext db, UserManager<AppUser> users, MailSender mail, EmailCodes codes) => {
            var email = input.Email?.Trim();
            var user = EmailValid(email) ? await users.FindByEmailAsync(email!) : null;
            if (user == null || !user.EmailConfirmed) return Results.Ok(new { challengeId = Guid.NewGuid(), message = "Se houver uma conta confirmada, enviaremos um código." });
            return await IssueAsync(db, mail, codes, user, "reset", null);
        });
        auth.MapPost("/verify", async (VerifyInput input, AppDbContext db, UserManager<AppUser> users, EmailCodes codes) => {
            if (input.Code is not { Length: 6 } || !input.Code.All(char.IsAsciiDigit))
                return Error("Informe o código de seis dígitos.");
            if (input.NewPassword is { Length: > 256 }) return Error("Use uma senha de até 256 caracteres.");
            // Serializable transaction stops parallel requests from exceeding the attempt limit or consuming a code twice.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var challenge = await db.EmailChallenges.FindAsync(input.ChallengeId);
            if (challenge == null || challenge.UsedAt != null || challenge.ExpiresAt < DateTime.UtcNow || challenge.Attempts >= 5) return Error("Código inválido ou expirado. Solicite um novo código.");
            challenge.Attempts++;
            if (!codes.Matches(challenge.Id, input.Code, challenge.CodeHash)) {
                await db.SaveChangesAsync(); await transaction.CommitAsync(); return Error("Código incorreto. Confira seu e-mail.");
            }
            var user = await users.FindByIdAsync(challenge.UserId);
            if (user == null) return Error("Cadastro não encontrado.");
            if (challenge.Purpose == "reset") {
                if (string.IsNullOrWhiteSpace(input.NewPassword)) return Error("Informe sua nova senha.");
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var reset = await users.ResetPasswordAsync(user, token, input.NewPassword);
                if (!reset.Succeeded) return Error("Use 10 caracteres, maiúscula, minúscula, número e símbolo.");
            } else {
                if (challenge.InvitationId != null) {
                    var inv = await db.Invitations.FindAsync(challenge.InvitationId);
                    if (inv == null || inv.UsedAt != null || inv.ExpiresAt < DateTime.UtcNow) return Error("Convite expirado. Peça um novo convite.");
                    var client = await db.Clients.FindAsync(inv.ClientId);
                    if (client == null || client.Email != user.Email || (client.UserId != null && client.UserId != user.Id)) return Error("Convite indisponível.");
                    client.UserId = user.Id; inv.UsedAt = DateTime.UtcNow;
                }
                if (string.IsNullOrWhiteSpace(input.NewPassword)) return Error("Defina sua senha de acesso.");
                var passwordToken = await users.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await users.ResetPasswordAsync(user, passwordToken, input.NewPassword);
                if (!passwordResult.Succeeded) return Error("Use 10 caracteres, maiúscula, minúscula, número e símbolo.");
                user.EmailConfirmed = true;
                await users.UpdateAsync(user);
            }
            challenge.UsedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { message = "Tudo pronto. Entre com seu e-mail e senha." });
        });
    }
    static async Task<IResult> IssueAsync(AppDbContext db, MailSender mail, EmailCodes codes, AppUser user, string purpose, Guid? invitationId)
    {
        var now = DateTime.UtcNow;
        if (await db.EmailChallenges.AnyAsync(x => x.UserId == user.Id && x.ExpiresAt > now.AddMinutes(9))) return Error("Aguarde um minuto antes de solicitar outro código.");
        var active = await db.EmailChallenges.Where(x => x.UserId == user.Id && x.Purpose == purpose && x.UsedAt == null).ToListAsync();
        foreach (var old in active) old.UsedAt = now;
        var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
        var challenge = new EmailChallenge { UserId = user.Id, Purpose = purpose, InvitationId = invitationId, ExpiresAt = now.AddMinutes(10) };
        challenge.CodeHash = codes.Protect(challenge.Id, code);
        db.EmailChallenges.Add(challenge); await db.SaveChangesAsync();
        try { await mail.SendCodeAsync(user.Email!, user.DisplayName, code, purpose); }
        catch (MailDeliveryException) { challenge.ExpiresAt = now; await db.SaveChangesAsync(); return Results.Json(new { error = "Não conseguimos enviar o e-mail. Confira o serviço de envio e solicite outro código." }, statusCode: 503); }
        return Results.Ok(new { challengeId = challenge.Id, message = "Confira seu e-mail." });
    }
}
