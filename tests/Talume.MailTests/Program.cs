using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Talume.Web.Services;

var root = Path.GetFullPath("src/Talume.Web/wwwroot");
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
    ["Mail:Provider"] = "Resend", ["Mail:From"] = "Talume <sender@example.com>",
    ["Resend:ApiKey"] = "test-key-never-sent"
}).Build();
var env = new TestEnvironment { WebRootPath = root, EnvironmentName = "Production" };
var handler = new CaptureHandler();
var sender = new MailSender(config, env, new TestClients(handler), NullLogger<MailSender>.Instance);
int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
async Task Fails(Func<Task> action, string name) {
    try { await action(); throw new Exception("Expected safe failure: " + name); }
    catch (MailDeliveryException ex) { Check(!ex.Message.Contains("test-key") && !ex.Message.Contains("private-provider-body"), name); }
}

await sender.SendAsync("recipient@example.com", "Seu código Talume", "Seu código é 123456.");
Check(handler.Uri == "https://api.resend.com/emails" && handler.Method == "POST", "Resend HTTPS endpoint");
Check(handler.Authorization == "Bearer test-key-never-sent", "Bearer authentication");
using (var payload = JsonDocument.Parse(handler.Body!)) {
    var p = payload.RootElement;
    Check(p.GetProperty("from").GetString() == config["Mail:From"] && p.GetProperty("to")[0].GetString() == "recipient@example.com", "sender and recipient");
    Check(p.GetProperty("text").GetString() == "Seu código é 123456." && !p.TryGetProperty("html", out _), "plain text verification/reset messages");
}
await sender.SendInvitationAsync("client@example.com", "Carlos <script>", "https://example.com/Account?invite=test");
using (var payload = JsonDocument.Parse(handler.Body!)) {
    var p = payload.RootElement;
    var html = p.GetProperty("html").GetString()!;
    Check(html.Contains("&lt;script&gt;") && !html.Contains("Carlos <script>"), "HTML escapes client names");
    Check(p.GetProperty("text").GetString()!.Contains("https://example.com/Account?invite=test"), "invitation includes text alternative");
    var attachments = p.GetProperty("attachments");
    Check(attachments.GetArrayLength() == 2, "two embedded invitation images");
    foreach (var a in attachments.EnumerateArray()) {
        var bytes = Convert.FromBase64String(a.GetProperty("content").GetString()!);
        Check(bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(root, "mail", a.GetProperty("filename").GetString()!))) && html.Contains("cid:" + a.GetProperty("content_id").GetString()), "attachment content and CID match");
    }
}
foreach (var status in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError }) {
    handler.Status = status;
    var before = handler.Calls;
    await Fails(() => sender.SendAsync("recipient@example.com", "subject", "body"), "safe HTTP " + (int)status + " failure");
    Check(handler.Calls == before + 1, "no automatic duplicate delivery after " + (int)status);
}
handler.Status = HttpStatusCode.OK;
handler.Failure = new HttpRequestException("private-provider-body");
await Fails(() => sender.SendAsync("recipient@example.com", "subject", "body"), "safe network failure");
handler.Failure = new TaskCanceledException("private-provider-body");
await Fails(() => sender.SendAsync("recipient@example.com", "subject", "body"), "safe timeout");
handler.Failure = null;
var calls = handler.Calls;
config["Resend:ApiKey"] = "";
await Fails(() => sender.SendAsync("recipient@example.com", "subject", "body"), "missing credentials");
Check(handler.Calls == calls, "missing key does not send HTTP");
config["Resend:ApiKey"] = "test-key-never-sent";
config["Mail:From"] = "talume@localhost";
await Fails(() => sender.SendAsync("recipient@example.com", "subject", "body"), "local sender rejected by Resend adapter");
config["Mail:Provider"] = "unknown";
await Fails(() => sender.SendAsync("recipient@example.com", "subject", "body"), "unknown provider fails closed");
config["Mail:Provider"] = null;
config["Mail:From"] = "sender@example.com";
await sender.SendAsync("recipient@example.com", "subject", "body");
Check(handler.Calls == calls + 1, "production defaults to Resend");
Check(!RegistrationPolicy.CanRegisterDeveloper("carlos@example.com", config, env), "pilot denies unlisted developers");
config["Registration:AllowedEmails"] = " other@example.com; CARLOS@example.com ";
Check(RegistrationPolicy.CanRegisterDeveloper("carlos@example.com", config, env), "allowlist trims and ignores email case");
Check(!RegistrationPolicy.CanRegisterDeveloper("stranger@example.com", config, env), "allowlist denies other developers");
env.EnvironmentName = "Development";
Check(RegistrationPolicy.CanRegisterDeveloper("stranger@example.com", config, env), "local development registration unchanged");
Console.WriteLine($"{passed} checks passed. No real emails sent.");

sealed class CaptureHandler : HttpMessageHandler {
    public string? Uri, Method, Authorization, Body;
    public int Calls;
    public HttpStatusCode Status = HttpStatusCode.OK;
    public Exception? Failure;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) {
        Calls++; Uri = request.RequestUri!.ToString(); Method = request.Method.Method;
        Authorization = request.Headers.Authorization?.ToString(); Body = await request.Content!.ReadAsStringAsync(token);
        if (Failure != null) throw Failure;
        return new HttpResponseMessage(Status) { Content = new StringContent("private-provider-body") };
    }
}
sealed class TestClients(CaptureHandler handler) : IHttpClientFactory {
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
sealed class TestEnvironment : IWebHostEnvironment {
    public string ApplicationName { get; set; } = "Talume.MailTests";
    public string EnvironmentName { get; set; } = "Production";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
