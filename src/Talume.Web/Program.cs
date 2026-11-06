using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Talume.Web.Data;
using Talume.Web.Models;
using Talume.Web.Services;
using Talume.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);
const int maxRequestBytes = 1024 * 1024;
builder.WebHost.ConfigureKestrel(o => {
    o.AddServerHeader = false;
    o.Limits.MaxRequestBodySize = maxRequestBytes;
});
var proxyAddresses = builder.Configuration.GetSection("Security:TrustedProxies").Get<string[]>() ?? [];
if (proxyAddresses.Length > 0) {
    builder.Services.Configure<ForwardedHeadersOptions>(o => {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        foreach (var address in proxyAddresses) o.KnownProxies.Add(IPAddress.Parse(address));
    });
}
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TenantConnectionInterceptor>();
builder.Services.AddDbContext<AppDbContext>((services, o) => {
    var connection = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Configure ConnectionStrings__Default.");
    if (builder.Configuration["Database:Provider"] == "Sqlite") {
        if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("SQLite is only available for local development.");
        o.UseSqlite(connection);
    } else {
        o.UseNpgsql(connection);
        if (builder.Configuration.GetValue<bool>("Database:TenantPolicies")) {
            var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
            if (settings.NoResetOnClose || settings.Multiplexing)
                throw new InvalidOperationException("Tenant policies require connection reset and exclusive connection use.");
            o.AddInterceptors(services.GetRequiredService<TenantConnectionInterceptor>());
        }
    }
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
builder.Services.AddAntiforgery(o => {
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddRazorPages(o => {
    o.Conventions.AuthorizeFolder("/");
    o.Conventions.AllowAnonymousToPage("/Account");
    o.Conventions.AllowAnonymousToPage("/Error");
    o.Conventions.AllowAnonymousToPage("/Privacy");
});
builder.Services.AddScoped<MailSender>();
builder.Services.AddSingleton<EmailCodes>();
builder.Services.AddHttpClient("Resend", client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:Path"] ?? ".keys")).SetApplicationName("Talume");
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c =>
        c.Request.Path.StartsWithSegments("/api/auth")
            ? RateLimitPartition.GetConcurrencyLimiter("auth", _ => new ConcurrencyLimiterOptions { PermitLimit = 8, QueueLimit = 0 })
            : RateLimitPartition.GetNoLimiter("other"));
    o.OnRejected = (context, _) => {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    };
    o.AddPolicy("auth", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("workspace", c => RateLimitPartition.GetFixedWindowLimiter(c.User.UserId() ?? "anonymous", _ =>
        new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
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
if (proxyAddresses.Length > 0) app.UseForwardedHeaders();
app.UseExceptionHandler(handler => handler.Run(async context => {
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    context.Response.StatusCode = error is BadHttpRequestException badRequest ? badRequest.StatusCode : 500;
    if (context.Request.Path.StartsWithSegments("/api")) await context.Response.WriteAsJsonAsync(new {
        error = context.Response.StatusCode == 413 ? "O conteúdo enviado excede o tamanho permitido." : "Não foi possível concluir. Confira a solicitação e tente novamente."
    });
    else await context.Response.WriteAsync("Não foi possível abrir esta página. Tente novamente.");
}));
app.Use(async (ctx, next) => {
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    // Railway terminates public TLS before forwarding to Kestrel.
    if (!app.Environment.IsDevelopment()) ctx.Response.Headers.StrictTransportSecurity = "max-age=31536000";
    if (!ctx.Request.Path.StartsWithSegments("/app.css") && !ctx.Request.Path.StartsWithSegments("/app.js")) ctx.Response.Headers.CacheControl = "no-store";
    if (ctx.Request.ContentLength > maxRequestBytes) {
        ctx.Response.StatusCode = 413;
        await ctx.Response.WriteAsJsonAsync(new { error = "O conteúdo enviado excede o tamanho permitido." });
        return;
    }
    await next();
});
app.UseStaticFiles();
app.UseRouting();
if (!app.Environment.IsDevelopment()) app.UseCookiePolicy(new CookiePolicyOptions { Secure = CookieSecurePolicy.Always });
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
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
