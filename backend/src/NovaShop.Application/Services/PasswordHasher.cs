using System.Security.Cryptography;

namespace NovaShop.Application.Services;

/// <summary>
/// PBKDF2 (Rfc2898DeriveBytes) password hasher. Hash format:
/// PBKDF2$iterations$base64(salt)$base64(subkey)
/// Salt is 16 bytes, subkey 32 bytes, 100_000 iterations (OWASP minimum).
/// Verification re-derives with the stored salt/iterations — constant-time
/// comparison via CryptographicOperations.FixedTimeEquals.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "PBKDF2";

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(subkey)}";
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix) return false;

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0) return false;

        byte[] salt, expectedSubkey;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedSubkey = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, expectedSubkey.Length);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}