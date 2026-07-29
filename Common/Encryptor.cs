using System.Security.Cryptography;
using System.Text;

namespace HeThongThiDQ.Common
{
    public class Encryptor
    {
        public static string MD5Hash(string text)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(text);
            byte[] hashBytes = MD5.HashData(inputBytes);

            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}
