using System.Security.Cryptography;

namespace ProjectPulse.Api.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}

// Formato que guardamos:  iterations.saltBase64.hashBase64
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        const int iterations = 100_000;                 // costo (seguridad vs tiempo)
        Span<byte> salt = stackalloc byte[16];          // 128-bit salt
        RandomNumberGenerator.Fill(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32                                           // 256-bit hash
        );

        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3) return false;

        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length
        );

        return CryptographicOperations.FixedTimeEquals(hash, expected);
    }
}
