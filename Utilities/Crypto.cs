using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SolarNG.Utilities;

internal class Crypto
{
    private static byte[] GenerateRandomEntropy(int bits)
    {
        byte[] array = new byte[bits / 8];
        using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
        rNGCryptoServiceProvider.GetBytes(array);
        return array;
    }

    public static byte[] Argon2dSalt()
    {
        return GenerateRandomEntropy(192);
    }

    public static byte[] Argon2dHash(string password, byte[] salt)
    {
        byte[] pwd = Encoding.UTF8.GetBytes(password);
        byte[] hash = new byte[32];

        SolarNGX.Argon2dHash(pwd, (uint)pwd.Length, salt, (uint)salt.Length, hash, (uint)hash.Length);

        return hash;
    }

    public static byte[] Encrypt(byte[] hash, byte[] salt, byte[] data)
    {
        byte[] iv = GenerateRandomEntropy(96);
        byte[] nonce = GenerateRandomEntropy(96);
        byte[] auth = new byte[16];
        data = AddPkcs7Padding(data);

        byte[] cipher = new byte[data.Length];

        SolarNGX.AESGCMCrypt(1, data, (uint)data.Length, hash, iv, nonce, auth, cipher, (uint)cipher.Length);

        return salt.Concat(iv).ToArray().Concat(nonce).ToArray().Concat(auth).ToArray().Concat(cipher).ToArray();
    }

    public static byte[] Encrypt(string password, byte[] data)
    {
        byte[] salt = Argon2dSalt();
         byte[] hash = Argon2dHash(password, salt);

        return Encrypt(hash, salt, data);
    }

    public static byte[] Decrypt(byte[] hash, byte[] cipher)
    {
        if (cipher.Length < (64 + 16))
        {
            return null;
        }

        byte[] iv = cipher.Skip(24).Take(12).ToArray();
        byte[] nonce = cipher.Skip(36).Take(12).ToArray();
        byte[] auth = cipher.Skip(48).Take(16).ToArray();

        int length = cipher.Length - 64;
        byte[] data = new byte[length];
        Buffer.BlockCopy(cipher, 64, data, 0, length);

        byte[] output = new byte[length];

        if (SolarNGX.AESGCMCrypt(0, data, (uint)data.Length, hash, iv, nonce, auth, output, (uint)output.Length) != (uint)length)
        {
            return null;
        }

        return RemovePkcs7Padding(output);
    }

    public static byte[] Decrypt(string password, byte[] cipher)
    {
        if(cipher.Length < (64+16))
        {
            return null;
        }

        byte[] salt = cipher.Take(24).ToArray();
        byte[] hash = Argon2dHash(password, salt);

        return Decrypt(hash, cipher);
    }

    private static byte[] AddPkcs7Padding(byte[] data)
    {
        int paddingLength = 16 - (data.Length % 16);

        byte[] output = new byte[data.Length + paddingLength];

        Buffer.BlockCopy(data, 0, output, 0, data.Length);

        for (var i = data.Length; i < output.Length; i++)
        {
            output[i] = (byte)paddingLength;
        }
        return output;
    }

    private static byte[] RemovePkcs7Padding(byte[] paddedByteArray)
    {
        if (paddedByteArray.Length < 16)
        {
            return null;
        }

        if ((paddedByteArray.Length % 16) != 0)
        {
            return null;
        }

        if (paddedByteArray.Length < paddedByteArray[paddedByteArray.Length - 1])
        {
            return null;
        }

        int length = paddedByteArray.Length - paddedByteArray[paddedByteArray.Length - 1];

        byte[] output = new byte[length];

        Buffer.BlockCopy(paddedByteArray, 0, output, 0, length);

        return output;
    }

}
