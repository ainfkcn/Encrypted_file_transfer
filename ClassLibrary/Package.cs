using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using static ClassLibrary.EnDe;
using System.Security.Cryptography;

namespace ClassLibrary
{
    [Serializable]
    public class Package
    {
        /// <summary>
        /// 通信包功能号
        /// </summary>
        public Service ServiceType;
        /// <summary>
        /// 通信包载荷，可为null
        /// </summary>
        public List<byte[]> PayLoad;

        /// <summary>
        /// 默认无参数构造函数，因通信协议的复杂性，暂不提供自动化构造函数，需要手动构造通信包
        /// </summary>
        public Package() { PayLoad = new List<byte[]>(); }

        /// <summary>
        /// 重写ToString，打印通信包的内容。测试数据包使用，在将来的版本会移除
        /// </summary>
        /// <returns>格式化后的通信包字符串</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ServiceType: " + ServiceType.ToString());
            foreach (var cargo in PayLoad) { sb.Append(" " + Decode(cargo)); }
            return sb.ToString();
        }

        /// <summary>
        /// 加密，序列化并发送数据包
        /// </summary>
        /// <param name="socket">Tcp套接字</param>
        /// <param name="pSend">待发送的数据包</param>
        /// <param name="key">AES密钥</param>
        /// <param name="rsa">RSA公钥</param>
        public static void Send(Socket socket, Package pSend, byte[] key, RSACryptoServiceProvider rsa)
        {
            //如果是注册，登陆，且有载荷，RSA加密
            if ((pSend.ServiceType == Service.Login
                || pSend.ServiceType == Service.Registration)
                && pSend.PayLoad.Count != 0)
                for (int i = 0; i < pSend.PayLoad.Count; i++)
                    pSend.PayLoad[i] = rsa.Encrypt(pSend.PayLoad[i], true);
            //如果不是注册，登陆，且有载荷，AES加密
            else if (pSend.ServiceType != Service.Login
                && pSend.ServiceType != Service.Registration
                && pSend.PayLoad.Count != 0)
                for (int i = 0; i < pSend.PayLoad.Count; i++)
                    pSend.PayLoad[i] = AesEncrypt(pSend.PayLoad[i], key);
            //二进制序列化Routine
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(mStream, pSend);
            mStream.Flush();
            //套接字发送
            _ = socket.Send(mStream.GetBuffer(), (int)mStream.Length, SocketFlags.None);
        }
        /// <summary>
        /// 接收数据包，反序列化并解密
        /// </summary>
        /// <param name="socket">Tcp套接字</param>
        /// <param name="key">AES密钥</param>
        /// <param name="rsa">RSA私钥</param>
        /// <returns>反序列化后的数据包</returns>
        public static Package Recive(Socket socket, byte[] key, RSACryptoServiceProvider rsa)
        {
            //套接字接收
            byte[] buffer = new byte[Size.BufferSize];
            int ret = socket.Receive(buffer);
            Package pRecive;
            //反序列化Routine
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter deformatter = new BinaryFormatter();
            mStream.Write(buffer, 0, ret);
            mStream.Flush();
            mStream.Seek(0, SeekOrigin.Begin);
            pRecive = deformatter.Deserialize(mStream) as Package;
            //如果是注册，登陆，且有载荷，RSA加密
            if ((pRecive.ServiceType == Service.Login
                || pRecive.ServiceType == Service.Registration)
                && pRecive.PayLoad.Count != 0)
                for (int i = 0; i < pRecive.PayLoad.Count; i++)
                    pRecive.PayLoad[i] = rsa.Decrypt(pRecive.PayLoad[i], true);
            //如果不是注册，登陆，且有载荷。AES解密
            else if (pRecive.ServiceType != Service.Login
                && pRecive.ServiceType != Service.Registration
                && pRecive.PayLoad.Count != 0)
                for (int i = 0; i < pRecive.PayLoad.Count; i++)
                    pRecive.PayLoad[i] = AesDecrypt(pRecive.PayLoad[i], key);
            return pRecive;
        }
    }
}