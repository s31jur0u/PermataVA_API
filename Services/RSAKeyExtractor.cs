using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public class RsaKeyExtractor
{
    public static RSA GetPrivateKey(string? pemFilePath)
    {
        var pemContent = File.ReadAllText(pemFilePath);
        return GetPrivateKeyFromPemContent(pemContent);
    }

    private static RSA GetPrivateKeyFromPemContent(string pemContent)
    {
        pemContent = pemContent.Replace("-----BEGIN RSA PRIVATE KEY-----", string.Empty)
                               .Replace("-----END RSA PRIVATE KEY-----", string.Empty)
                               .Replace("\n", string.Empty)
                               .Replace("\r", string.Empty);

        var privateKeyBytes = Convert.FromBase64String(pemContent);
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        return rsa;
    }


    public static RSA GetPublicKey(string? pemFilePath)
    {
        var pemContent = File.ReadAllText(pemFilePath);
        return GetPublicKeyFromPemContent(pemContent);
    }

    private static RSA GetPublicKeyFromPemContent(string pemContent)
    {
        pemContent = pemContent.Replace("-----BEGIN PUBLIC KEY-----", string.Empty)
                               .Replace("-----END PUBLIC KEY-----", string.Empty)
                               .Replace("\n", string.Empty)
                               .Replace("\r", string.Empty);

        var publicKeyBytes = Convert.FromBase64String(pemContent);
        var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
        return rsa;
    }
}
