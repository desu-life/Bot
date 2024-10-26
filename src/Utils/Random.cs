using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KanonBot;

public static partial class Utils
{
    private static RandomNumberGenerator rng = RandomNumberGenerator.Create();
  
      public static int RandomNum(int min, int max)
    {
        var r = new Random(
            DateTime.Now.Millisecond
                + DateTime.Now.Second
                + DateTime.Now.Minute
                + DateTime.Now.Microsecond
                + DateTime.Now.Nanosecond
        );
        return r.Next(min, max);
    }
  
  public static byte[] GenerateRandomBytes(int length)
    {
        byte[] randomBytes = new byte[length];
        rng.GetBytes(randomBytes);
        return randomBytes;
    }

      public static string RandomStr(int length, bool URLparameter = false)
    {
        string str = "";
        str += "0123456789";
        str += "abcdefghijklmnopqrstuvwxyz";
        str += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (!URLparameter)
            str += "!_-@#$%+^&()[]'~`";
        StringBuilder sb = new();
        for (int i = 0; i < length; i++)
        {
            byte[] randomBytes = GenerateRandomBytes(100);
            int randomIndex = randomBytes[i] % str.Length;
            sb.Append(str[randomIndex]);
        }
        return sb.ToString();
    }

    public static string RandomRedemptionCode()
    {
        StringBuilder sb = new();
        string str = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (int o = 0; o < 5; o++)
        {
            for (int i = 0; i < 5; i++)
            {
                byte[] randomBytes = GenerateRandomBytes(255);
                int randomIndex = randomBytes[i] % str.Length;
                sb.Append(str[randomIndex]);
            }
            if (o < 4) sb.Append('-');
        }
        return sb.ToString();
    }
}
