using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharedCore
{
    public class CryptoService
    {
        private ECDiffieHellmanCng _ecdh;
        public byte[] PublicKey { get; private set; }
        public byte[] SharedSecret { get; private set; }

        public CryptoService()
        {
            // Khởi tạo thuật toán Elliptic Curve Diffie-Hellman
            _ecdh = new ECDiffieHellmanCng();
            _ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            _ecdh.HashAlgorithm = CngAlgorithm.Sha256;

            // Lấy Public Key của mình để chuẩn bị gửi cho người kia
            PublicKey = _ecdh.PublicKey.ToByteArray();
        }

        // Hàm này nhận Public Key của người kia để tạo ra Chìa khóa bí mật chung (Shared Secret)
        public void DeriveSharedSecret(byte[] otherPartyPublicKey)
        {
            CngKey otherKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob);
            SharedSecret = _ecdh.DeriveKeyMaterial(otherKey);
        }

        // Hàm dùng AES để MÃ HÓA tin nhắn chữ thành mảng byte lộn xộn
        public byte[] EncryptAES(string plainText)
        {
            if (SharedSecret == null) throw new Exception("Chưa tạo khóa bí mật chung!");

            using (Aes aes = Aes.Create())
            {
                aes.Key = SharedSecret;
                aes.GenerateIV(); // Tạo vector ngẫu nhiên cho mỗi tin nhắn

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length); // Gắn IV vào đầu tin nhắn
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        // Hàm dùng AES để GIẢI MÃ mảng byte lộn xộn trở lại thành chữ đọc được
        public string DecryptAES(byte[] cipherText)
        {
            if (SharedSecret == null) throw new Exception("Chưa tạo khóa bí mật chung!");

            using (Aes aes = Aes.Create())
            {
                aes.Key = SharedSecret;

                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    byte[] iv = new byte[aes.BlockSize / 8];
                    ms.Read(iv, 0, iv.Length); // Đọc IV từ đầu tin nhắn ra
                    aes.IV = iv;

                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (StreamReader reader = new StreamReader(cs))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}