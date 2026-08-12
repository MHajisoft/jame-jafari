using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace JameJafari.Infrastructure.Security;

/// <summary>
/// ASP.NET Identity password hasher with legacy SHA256 Base64 verify for existing users.
/// </summary>
public sealed class AppPasswordHasher : IAppPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string hash, out bool needsRehash)
    {
        needsRehash = false;
        if (string.IsNullOrEmpty(hash))
            return false;

        // Identity V3 hashes are Base64 and typically start with "AQAAAA"
        if (hash.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            var result = _hasher.VerifyHashedPassword(null!, hash, password);
            needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }

        // Legacy: unsalted SHA256 → Base64
        var legacy = HashLegacySha256(password);
        if (!string.Equals(legacy, hash, StringComparison.Ordinal))
            return false;

        needsRehash = true;
        return true;
    }

    private static string HashLegacySha256(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
