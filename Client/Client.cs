using ClassLibrary;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Console;

namespace Client
{
    public class Client
    {
        /// <summary>
        /// 通信中使用的Tcp套接字
        /// </summary>
        private readonly Socket socket;
        private readonly IPEndPoint remoteEP;

        /// <summary>
        /// 默认无参数构造函数，初始化Tcp套接字
        /// </summary>
        public Client()
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            remoteEP = new IPEndPoint(ipAddress, 11000);
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// 客户端启动，连接后开始进入交互函数
        /// </summary>
        public void Start()
        {
            try
            {
                socket.Connect(remoteEP);
                WriteLine("Socket connected to {0}", socket.RemoteEndPoint.ToString());
                Shell();
            }
            catch (Exception e) { WriteLine(e.ToString()); }
            finally
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        /// <summary>
        /// 构想中的交互函数，无参型；有参型用来做shell
        /// </summary>
        public void Shell()
        {
            try
            {
                while (true)
                {
                    WriteLine("1.Login");
                    Write("Client>");
                    string op = ReadLine().ToLower();
                    //登录部分
                    if (op.Contains("l") || op.Contains("log") || op.Equals("1"))
                    {
                        //登录数据包构造
                        Package pSend = new Package { ServiceType = Service.Login };
                        Write("username:");
                        pSend.PayLoad.Add(Encoding.UTF8.GetBytes(ReadLine()));
                        Write("paaaword:");
                        pSend.PayLoad.Add(Encoding.UTF8.GetBytes(ReadLine()));
                        //发送&接收
                        Package.Send(socket, pSend);
                        Package pBack = Package.Recive(socket);
                        WriteLine(pBack);
                    }
                }
            }
            catch (Exception e) { WriteLine(e.ToString()); }
        }
    }
}
