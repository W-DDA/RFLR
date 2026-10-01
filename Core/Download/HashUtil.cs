using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyLauncher.Core.Download
{
    public static class HashUtil
    {
        public static string Hash(string filePath, string algo)
        {
            using var stream = File.OpenRead(filePath);
            using var md = algo.ToUpper() switch
            {
                "SHA-1" => (HashAlgorithm)SHA1.Create(),
                "SHA-256" => SHA256.Create(),
                _ => SHA1.Create()
            };
            var bytes = md.ComputeHash(stream);
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}