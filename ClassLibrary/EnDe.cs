using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace ClassLibrary
{
    public static class EnDe
    {
        /// <summary>
        /// 编码函数，用来将字符串编码为字节串
        /// </summary>
        /// <param name="str">要编码的字符串</param>
        /// <returns>编码结果的字节串</returns>
        public static byte[] Encode(string str) => Encoding.UTF8.GetBytes(str);
        /// <summary>
        /// 解码函数，用来将字节串解码为字符串
        /// </summary>
        /// <param name="bytes">要解码的字节串</param>
        /// <returns>解码结果的字符串</returns>
        public static string Decode(byte[] bytes) => Encoding.UTF8.GetString(bytes);
        /// <summary>
        /// AES加密函数
        /// </summary>
        /// <param name="message">待加密的字节串明文信息</param>
        /// <param name="key">加密密钥</param>
        /// <returns>加密后的字节串密文</returns>
        public static byte[] AesEncrypt(byte[] message, byte[] key)
        {
            using (var rijndael = new RijndaelManaged()
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = key.Skip(32).Take(16).ToArray(),
                BlockSize = 128,
                KeySize = 256,
            })
            {
                rijndael.Key = key.Take(32).ToArray();
                ICryptoTransform cryptoTransform = rijndael.CreateEncryptor();
                return cryptoTransform.TransformFinalBlock(message, 0, message.Length);
            }
        }
        /// <summary>
        /// AES解密函数
        /// </summary>
        /// <param name="cipher">待解密的字节串密文</param>
        /// <param name="key">解密密钥</param>
        /// <returns>解密后的字节串明文</returns>
        public static byte[] AesDecrypt(byte[] cipher, byte[] key)
        {
            using (var rijndael = new RijndaelManaged()
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = key.Skip(32).Take(16).ToArray(),
                BlockSize = 128,
                KeySize = 256,
            })
            {
                rijndael.Key = key.Take(32).ToArray();
                ICryptoTransform cryptoTransform = rijndael.CreateDecryptor();
                return cryptoTransform.TransformFinalBlock(cipher, 0, cipher.Length);
            }
        }
    }
}
