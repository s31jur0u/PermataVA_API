namespace VA_API.Services;


using System.Security.Cryptography;
using System.Text;

public class Encryption
{
    public static byte[] Encrypt(string plainText, string secretKey)
    {
        using (Aes aes = Aes.Create())
        {
            // Atur kunci dan IV
            aes.Key = GetKey(secretKey);
            aes.GenerateIV(); // IV di-generate otomatis

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream())
            {
                // Simpan IV di awal data
                ms.Write(aes.IV, 0, aes.IV.Length);
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return ms.ToArray();
            }
        }
    }
    public static string Decrypt(byte[] cipherText, string secretKey)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] fullCipher = cipherText;

            // Ekstrak IV dari data
            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] cipher = new byte[fullCipher.Length - iv.Length];
            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.Key = GetKey(secretKey);
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream(cipher))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
    private static byte[] GetKey(string secretKey)
    {
        // Hash secret key untuk mendapatkan ukuran 256-bit (32 bytes)
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));
        }
    }

}
