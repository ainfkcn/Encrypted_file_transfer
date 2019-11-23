using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using static ClassLibrary.EnDe;

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
        public Package()
        {
            PayLoad = new List<byte[]>();
        }

        /// <summary>
        /// 重写ToString，打印通信包的内容
        /// </summary>
        /// <returns>格式化后的通信包字符串</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ServiceType: " + ServiceType.ToString());
            foreach (var cargo in PayLoad) { sb.Append("\t" + Decode(cargo)); }
            return sb.ToString();
        }

        /// <summary>
        /// 静态方法，负责序列化并发送数据包
        /// </summary>
        /// <param name="socket">Tcp套接字</param>
        /// <param name="pSend">待发送的数据包</param>
        public static void Send(Socket socket, Package pSend)
        {
            //二进制序列化Routine
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(mStream, pSend);
            mStream.Flush();
            //套接字发送
            _ = socket.Send(mStream.GetBuffer(), (int)mStream.Length, SocketFlags.None);
        }

        /// <summary>
        /// 静态方法，接收数据包并反序列化，将数据包返回给上级函数进行处理
        /// </summary>
        /// <param name="socket">Tcp套接字</param>
        /// <returns>反序列化后的数据包</returns>
        public static Package Recive(Socket socket)
        {
            //套接字接收
            byte[] buffer = new byte[Size.BufferSize];
            int ret = socket.Receive(buffer);
            //反序列化Routine
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter deformatter = new BinaryFormatter();
            mStream.Write(buffer, 0, ret);
            mStream.Flush();
            mStream.Seek(0, SeekOrigin.Begin);
            return deformatter.Deserialize(mStream) as Package;
        }
    }
}
