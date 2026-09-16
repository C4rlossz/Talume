using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Talume.Web.Data;
using Talume.Web.Models;
using Talume.Web.Services;
using Talume.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(o => {
    var connection = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Configure ConnectionStrings__Default.");
    if (builder.Configuration["Database:Provider"] == "Sqlite") {
        if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("SQLite is only available for local development.");
        o.UseSqlite(connection);
    } else o.UseNpgsql(connection);
});
builder.Services.AddIdentity<AppUser, IdentityRole>(o => {
    o.User.RequireUniqueEmail = true;
    o.SignIn.RequireConfirmedEmail = true;
    o.Password.RequiredLength = 10;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o => {
    o.LoginPath = "/Account"; o.Cookie.Name = "Talume.Session";
    o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.Events.OnRedirectToLogin = ctx => { if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 401; else ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(o => o.AddPolicy("Freelancer", p => p.RequireRole("Freelancer")));
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddRazorPages(o => { o.Conventions.AuthorizeFolder("/"); o.Conventions.AllowAnonymousToPage("/Account"); o.Conventions.AllowAnonymousToPage("/Error"); });
builder.Services.AddScoped<MailSender>();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:Path"] ?? ".keys")).SetApplicationName("Talume");
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddPolicy("auth", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
if (app.Configuration["Tools:ExportInvitationPreview"] is string invitationPath) {
    string Asset(string name) => "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(Path.Combine(app.Environment.WebRootPath, "mail", name)));
    await File.WriteAllTextAsync(invitationPath, InvitationEmail.Html("Marina Costa", "#convite-de-demonstracao", Asset("talume-mark.png"), Asset("invitation-hero.png")));
    return;
}
if (app.Configuration["Tools:ExportSchema"] is string schemaPath) {
    using var scope = app.Services.CreateScope();
    await File.WriteAllTextAsync(schemaPath, scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GenerateCreateScript());
    return;
}
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseExceptionHandler(handler => handler.Run(async context => {
    context.Response.StatusCode = 500;
    if (context.Request.Path.StartsWithSegments("/api")) await context.Response.WriteAsJsonAsync(new { error = "Não foi possível concluir. Tente novamente ou consulte os logs do serviço." });
    else await context.Response.WriteAsync("Não foi possível abrir esta página. Tente novamente.");
}));
app.Use(async (ctx, next) => {
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "same-origin";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    if (!ctx.Request.Path.StartsWithSegments("/app.css") && !ctx.Request.Path.StartsWithSegments("/app.js")) ctx.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// Every mutation, including login, requires a same-origin anti-forgery token.
app.Use(async (ctx, next) => {
    if (ctx.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(ctx.Request.Method)) {
        try { await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx); }
        catch (AntiforgeryValidationException) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsJsonAsync(new { error = "Sessão expirada. Atualize a página." }); return; }
    }
    await next();
});
app.MapGet("/health", async (AppDbContext db) => await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503)).AllowAnonymous();
app.MapAuthEndpoints();
app.MapBusinessEndpoints();
app.MapRazorPages();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Configuration.GetValue("Database:Initialize", false)) {
        await db.Database.EnsureCreatedAsync();
        await SchemaUpdates.ApplyLocalAsync(db);
    }
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Freelancer", "Client" }) if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
    if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Demo:Seed", false)) await Seed.RunAsync(scope.ServiceProvider);
}
app.Run();
public partial class Program { }
