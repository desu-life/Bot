using System.Security.Cryptography;
using System.Text;

namespace KanonBot;

public static partial class Utils
{
    public static int RandomNum(int min, int max) => Random.Shared.Next(min, max);

    public static byte[] GenerateRandomBytes(int length)
    {
        byte[] randomBytes = new byte[length];
        RandomNumberGenerator.Fill(randomBytes);
        return randomBytes;
    }

    public static string RandomStr(int length, bool URLparameter = false)
    {
        ReadOnlySpan<char> chars = URLparameter
            ? "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
            : "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!_-@#$%+^&()[]'~`";

        Span<byte> randomBytes = length <= 256 ? stackalloc byte[length] : new byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        return string.Create(length, (chars.ToString(), randomBytes.ToArray()), static (span, state) =>
        {
            var (c, bytes) = state;
            for (int i = 0; i < span.Length; i++)
                span[i] = c[bytes[i] % c.Length];
        });
    }

    public static string RandomRedemptionCode()
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Span<byte> randomBytes = stackalloc byte[25];
        RandomNumberGenerator.Fill(randomBytes);

        return string.Create(29, randomBytes.ToArray(), static (span, bytes) =>
        {
            int byteIdx = 0;
            for (int o = 0; o < 5; o++)
            {
                for (int i = 0; i < 5; i++)
                    span[o * 6 + i] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[bytes[byteIdx++] % 36];
                if (o < 4)
                    span[o * 6 + 5] = '-';
            }
        });
    }
}
