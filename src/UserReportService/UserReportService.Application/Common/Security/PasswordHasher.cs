using System.Security.Cryptography;

namespace UserReportService.Application.Common.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16; // 128-bit
    private const int KeySize = 32;  // 256-bit
    private const int Iterations = 100000; // PBKDF2 standard iterations
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Mã hóa mật khẩu sử dụng PBKDF2-SHA256 với Salt ngẫu nhiên bảo mật.
    /// Sử dụng API tĩnh Rfc2898DeriveBytes.Pbkdf2 hiệu năng cao của .NET.
    /// </summary>
    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        // Định dạng lưu trữ: [Iterations (4 bytes)] + [SaltSize (4 bytes)] + [Salt] + [Subkey]
        byte[] result = new byte[8 + SaltSize + KeySize];

        Buffer.BlockCopy(BitConverter.GetBytes(Iterations), 0, result, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(SaltSize), 0, result, 4, 4);
        Buffer.BlockCopy(salt, 0, result, 8, SaltSize);
        Buffer.BlockCopy(key, 0, result, 8 + SaltSize, KeySize);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Kiểm tra mật khẩu gốc nhập vào có khớp với mã Hash lưu trữ hay không.
    /// Sử dụng so sánh an toàn constant-time FixedTimeEquals để ngăn chặn timing attacks.
    /// </summary>
    public static bool VerifyPassword(string hashedPassword, string password)
    {
        try
        {
            byte[] decoded = Convert.FromBase64String(hashedPassword);

            int iterations = BitConverter.ToInt32(decoded, 0);
            int saltSize = BitConverter.ToInt32(decoded, 4);

            byte[] salt = new byte[saltSize];
            Buffer.BlockCopy(decoded, 8, salt, 0, saltSize);

            int keySize = decoded.Length - 8 - saltSize;
            byte[] storedKey = new byte[keySize];
            Buffer.BlockCopy(decoded, 8 + saltSize, storedKey, 0, keySize);

            byte[] computedKey = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithm,
                keySize);

            return CryptographicOperations.FixedTimeEquals(storedKey, computedKey);
        }
        catch
        {
            return false;
        }
    }
}
