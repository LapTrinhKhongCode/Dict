using System.Security.Cryptography;
using System.Text;

public static class ZaloPayHelper
{
    public static string ComputeHmacSha256(string data, string key)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder();

            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
