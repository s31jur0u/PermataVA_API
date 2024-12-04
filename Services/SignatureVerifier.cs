using System.Security.Cryptography;
using System.Text;

public class SignatureVerifier
{
    public static bool VerifySignatureSha256(string data, RSA publicKey, string signature)
    {
        // Convert the data to a byte array
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] signatureBytes = Convert.FromBase64String(signature);

        // Verify the signature
        bool verified = publicKey.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return verified;

    }

    public static string CreateSignatureSha256(string data, RSA rsa)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] signedBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signedBytes);
    }
    public static bool VerifyHmacSha512(string data, string receivedHmac, string secretKey)
    {
        byte[] receivedHmacBytes = Convert.FromBase64String(receivedHmac);
        byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        // Convert the data and secret key to byte arrays
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        // using (var hmac = new HMACSHA512(secretKeyBytes))
        // {
        //     // Compute the HMAC
        //     byte[] computedHmac = hmac.ComputeHash(dataBytes);
        //
        //     // Compare the computed HMAC with the received HMAC
        //     return ByteArraysEqual(computedHmac, receivedHmacBytes);
        // }
        //
        using (var hmacsha512 = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey)))
        {
            byte[] hash = hmacsha512.ComputeHash(Encoding.UTF8.GetBytes(data));
            string tosignhmac = Convert.ToBase64String(hash);

            return tosignhmac == receivedHmac; // Convert to lowercase hex
        }
    }
    public static string CreateHmacSha512(string data, string secretKey)
    {
        byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        // Convert the data and secret key to byte arrays
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using (var hmac = new HMACSHA512(secretKeyBytes))
        {
            // Compute the HMAC
            byte[] computedHmac = hmac.ComputeHash(dataBytes);
            string signature = Convert.ToBase64String(computedHmac);
            return signature;
        }
    }

    // Method to compare two byte arrays
    private static bool ByteArraysEqual(byte[] a1, byte[] a2)
    {
        if (a1 == a2)
            return true;

        if (a1 == null || a2 == null)
            return false;

        if (a1.Length != a2.Length)
            return false;

        for (int i = 0; i < a1.Length; i++)
        {
            if (a1[i] != a2[i])
                return false;
        }

        return true;
    }
}