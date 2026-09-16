using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Talume.Web.Data;
using Talume.Web.Models;
namespace Talume.Web.Services;
public static class Access
{
    public static string UserId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public static bool Freelancer(this ClaimsPrincipal user) => user.IsInRole("Freelancer");
    public static IQueryable<Project> VisibleProjects(this AppDbContext db, ClaimsPrincipal user) =>
        user.Freelancer() ? db.Projects.Where(x => x.OwnerId == user.UserId()) : db.Projects.Where(x => x.Client.UserId == user.UserId());
    public static IQueryable<Quote> VisibleQuotes(this AppDbContext db, ClaimsPrincipal user) =>
        user.Freelancer() ? db.Quotes.Where(x => x.OwnerId == user.UserId()) : db.Quotes.Where(x => x.Client.UserId == user.UserId() && x.Status != "Rascunho");
    public static readonly string[] Stages = ["Briefing", "Em produção", "Em revisão", "Entregue"];
    public static bool HttpsUrl(string text) => Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo);
}
