using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Inkers.GerenciadorCampanhas.Services.Criptografia;


public class CriptografiaService
{
    private readonly string _secretKey;

    public CriptografiaService(IConfiguration configuration)
    {
        _secretKey = configuration["SECRET_KEY"] ?? 
            throw new ArgumentException("SECRET_KEY não encontrado.");
    }

    public string Descriptografar(string tokenCriptografado)
    {
        // 1. Decodifica o envelope externo do Flet (Base64 URL-Safe)
        byte[] rawBytes = Base64UrlDecode(tokenCriptografado);

        // 2. Extrai o Salt (16 bytes) e o Token do Fernet (restante)
        byte[] salt = rawBytes[0..16];
        byte[] fernetTokenBytes = rawBytes[16..];

        // 3. O token Fernet é uma string Base64 URL-Safe, decodificamos de novo
        string fernetStr = Encoding.UTF8.GetString(fernetTokenBytes);
        byte[] tokenDecoded = Base64UrlDecode(fernetStr);

        // 4. Deriva a chave de 32 bytes com PBKDF2 (600.000 iterações, SHA256)
        using var rfc = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(_secretKey), salt, 600000, HashAlgorithmName.SHA256);
        byte[] derivedKey = rfc.GetBytes(32);

        // O padrão Fernet usa a segunda metade da chave (16 bytes) para criptografia
        byte[] encryptionKey = derivedKey[16..32];

        // 5. Desmembra o token Fernet
        // Formato: Versão(1 byte) | Timestamp(8 bytes) | IV(16 bytes) | Ciphertext(N) | HMAC(32 bytes)
        byte[] iv = tokenDecoded[9..25];
        byte[] cipherText = tokenDecoded[25..^32];

        // 6. Descriptografa com AES-128-CBC
        using var aes = Aes.Create();
        aes.Key = encryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    // Método auxiliar obrigatório para lidar com o Base64 URL-Safe do Python
    private static byte[] Base64UrlDecode(string input)
    {
        string base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}