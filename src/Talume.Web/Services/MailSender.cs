using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
namespace Talume.Web.Services;
public class MailSender(IConfiguration config, IWebHostEnvironment env)
{
    public Task SendAsync(string to, string subject, string body) => SendMessageAsync(to, subject, body, null);
    public Task SendInvitationAsync(string to, string name, string url) =>
        SendMessageAsync(to, "Seu projeto tem um espaço no Talume", InvitationEmail.Text(name, url), InvitationEmail.Html(name, url));
    private async Task SendMessageAsync(string to, string subject, string body, string? html)
    {
        using var client = new SmtpClient(config["Mail:Host"] ?? "localhost", config.GetValue("Mail:Port", 1025));
        client.EnableSsl = config.GetValue("Mail:UseTls", false);
        if (!string.IsNullOrWhiteSpace(config["Mail:Username"]))
            client.Credentials = new NetworkCredential(config["Mail:Username"], config["Mail:Password"]);
        using var message = new MailMessage(config["Mail:From"] ?? "talume@localhost", to) { Subject = subject, SubjectEncoding = Encoding.UTF8, BodyEncoding = Encoding.UTF8 };
        if (html == null) message.Body = body;
        else {
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, MediaTypeNames.Text.Plain));
            var view = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html);
            message.AlternateViews.Add(view);
            foreach (var (file, id) in new[] { ("talume-mark.png", "talume-mark"), ("invitation-hero.png", "talume-hero") }) {
                var image = new LinkedResource(Path.Combine(env.WebRootPath, "mail", file), "image/png") { ContentId = id, TransferEncoding = TransferEncoding.Base64 };
                view.LinkedResources.Add(image);
            }
        }
        await client.SendMailAsync(message);
    }
}
