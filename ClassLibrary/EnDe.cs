using System.Text;

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
    }
}
