using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Talume.Web.Data;
using Talume.Web.Models;
using Talume.Web.Services;
namespace Talume.Web.Pages;
public class DocumentModel(AppDbContext db) : PageModel
{
    public Quote Quote { get; private set; } = null!;
    public string LogoDataUrl { get; private set; } = "";
    public BusinessProfile Issuer { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var quote = await db.VisibleQuotes(User).Include(x => x.Client).Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id);
        if (quote == null) return NotFound();
        Quote = quote;
        var owner = (await db.Users.FindAsync(quote.OwnerId))!;
        Issuer = await db.BusinessProfiles.FindAsync(quote.OwnerId) ?? new BusinessProfile { BusinessName = owner.DisplayName, ResponsibleName = owner.DisplayName };
        var savedLogo = await db.QuoteLogos.FindAsync(quote.Id);
        LogoDataUrl = savedLogo?.DataUrl ?? (await db.BusinessLogos.FindAsync(quote.OwnerId))?.DataUrl ?? "";
        return Page();
    }
}
