using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
namespace Talume.Web.Services;
public class MailSender(IConfiguration config, IWebHostEnvironment env, IHttpClientFactory clients, ILogger<MailSender> logger)
{
    public Task SendAsync(string to, string subject, string body) => SendMessageAsync(to, subject, body, null);
    public Task SendCodeAsync(string to, string name, string code, string purpose) =>
        SendMessageAsync(to, VerificationEmail.Subject(purpose), VerificationEmail.Text(name, code, purpose), VerificationEmail.Html(name, code, purpose));
    public Task SendInvitationAsync(string to, string name, string url) =>
        SendMessageAsync(to, "Seu projeto tem um espaço no Talume", InvitationEmail.Text(name, url), InvitationEmail.Html(name, url));
    private async Task SendMessageAsync(string to, string subject, string body, string? html)
    {
        var provider = config["Mail:Provider"] ?? (env.IsDevelopment() ? "Smtp" : "Resend");
        try {
            if (provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
                await SendResendAsync(to, subject, body, html);
            else if (provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
                await SendSmtpAsync(to, subject, body, html);
            else throw new MailDeliveryException("Provedor de e-mail inválido. Configure Mail__Provider.");
        }
        catch (Exception ex) when (ex is SmtpException or HttpRequestException or TaskCanceledException) {
            logger.LogWarning("Falha no transporte de e-mail ({ErrorType}).", ex.GetType().Name);
            throw new MailDeliveryException("Serviço de e-mail temporariamente indisponível.");
        }
    }

    private async Task SendResendAsync(string to, string subject, string body, string? html)
    {
        var key = config["Resend:ApiKey"];
        var from = config["Mail:From"];
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(from) ||
            !MailAddress.TryCreate(from, out var address) || address.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            throw new MailDeliveryException("Configure Resend__ApiKey e Mail__From com um remetente autorizado.");

        var payload = new Dictionary<string, object> {
            ["from"] = from, ["to"] = new[] { to }, ["subject"] = subject, ["text"] = body
        };
        if (html != null) {
            payload["html"] = html;
            var attachments = new List<object>();
            foreach (var (file, id) in new[] { ("talume-mark.png", "talume-mark"), ("invitation-hero.png", "talume-hero") }) {
                var bytes = await File.ReadAllBytesAsync(Path.Combine(env.WebRootPath, "mail", file));
                attachments.Add(new { filename = file, content = Convert.ToBase64String(bytes), content_type = "image/png", content_id = id });
            }
            payload["attachments"] = attachments;
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = JsonContent.Create(payload);
        using var client = clients.CreateClient("Resend");
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) {
            // Do not log provider bodies: they may contain recipient data or sensitive content.
            logger.LogWarning("Resend recusou o envio com HTTP {StatusCode}.", (int)response.StatusCode);
            throw new MailDeliveryException("O provedor de e-mail recusou o envio.");
        }
    }

    private async Task SendSmtpAsync(string to, string subject, string body, string? html)
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
