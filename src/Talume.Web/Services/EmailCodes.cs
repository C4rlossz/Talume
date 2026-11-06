using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Talume.Web.Endpoints;

namespace Talume.Web.Services;

public sealed class EmailCodes(IDataProtectionProvider protection)
{
    private const string Prefix = "protected:";
    private readonly IDataProtector protector = protection.CreateProtector("Talume.EmailCodes.v1");

    public string Protect(Guid challengeId, string code) =>
        Prefix + protector.Protect(AuthEndpoints.Hash(challengeId + ":" + code));

    public bool Matches(Guid challengeId, string code, string stored)
    {
        try {
            // Existing codes issued shortly before rollout still expire under the original ten-minute limit.
            var expected = stored.StartsWith(Prefix, StringComparison.Ordinal) ? protector.Unprotect(stored[Prefix.Length..]) : stored;
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected),
                Convert.FromHexString(AuthEndpoints.Hash(challengeId + ":" + code)));
        } catch (Exception error) when (error is CryptographicException or FormatException) {
            return false;
        }
    }
}
