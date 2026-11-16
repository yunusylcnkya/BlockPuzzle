using System;
using System.Security.Cryptography;
using System.Text;

namespace BlockPuzzle.ClassicPersistence
{
    // SHA-256 detects partial/corrupted saves; it is not anti-cheat encryption.
    public static class ClassicSaveIntegrity
    {
        public static string Hash(string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            using (SHA256 sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(digest);
            }
        }

        public static bool Matches(string data, string expected)
        {
            if (data == null || string.IsNullOrEmpty(expected)) return false;
            return string.Equals(Hash(data), expected, StringComparison.Ordinal);
        }
    }
}
