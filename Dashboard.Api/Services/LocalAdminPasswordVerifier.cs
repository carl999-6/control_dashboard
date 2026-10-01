using System.Security.Cryptography;
using System.Text;

namespace Dashboard.Api.Services;

public sealed class LocalAdminPasswordVerifier
{
    private const int Iterations = 120_000;
    private readonly byte[] _salt = RandomNumberGenerator.GetBytes(24);
    private readonly byte[] _expectedHash;

    public LocalAdminPasswordVerifier(string password)
    {
        _expectedHash = Derive(password);
    }

    public bool Verify(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        return CryptographicOperations.FixedTimeEquals(_expectedHash, Derive(password));
    }

    private byte[] Derive(string password) => Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(password), _salt, Iterations, HashAlgorithmName.SHA256, 32);
}
