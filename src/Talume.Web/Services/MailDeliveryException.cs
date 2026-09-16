namespace Talume.Web.Services;

// Contains only a safe diagnostic, never provider responses, addresses, codes or API keys.
public sealed class MailDeliveryException(string message) : Exception(message) { }
