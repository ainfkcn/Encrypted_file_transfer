using ClassLibrary;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static System.Console;

namespace Server
{
    public class Server
    {
        /// <summary>
        /// 用于存储每个线程对应套接字
        /// </summary>
        private readonly Dictionary<Thread, Socket> ThreadSocketPairs;
        /// <summary>
        /// 服务器IP地址
        /// </summary>
        private readonly IPAddress ipAddress;
        /// <summary>
        /// 服务器IP地址+端口，用于套接字bind
        /// </summary>
        private readonly IPEndPoint localEndPoint;

        /// <summary>
        /// 无参构造函数，对变量进行赋值和初始化
        /// </summary>
        public Server()
        {
            ThreadSocketPairs = new Dictionary<Thread, Socket>();
            ipAddress = IPAddress.Parse("127.0.0.1");
            localEndPoint = new IPEndPoint(ipAddress, 11000);
        }

        /// <summary>
        /// 服务器启动函数，无限循环负责侦听。有新链接时交给子线程去处理
        /// </summary>
        public void Start()
        {
            using (Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);
                    while (true)
                    {
                        WriteLine("Waiting for a connection...");
                        Thread workingThread = new Thread(Process);
                        ThreadSocketPairs.Add(workingThread, listener.Accept());
                        workingThread.Start();
                    }
                }
                catch (Exception e) { WriteLine(e.ToString()); }
            }
        }

        /// <summary>
        /// 服务器线程函数，处理交互逻辑，一个线程对应一个客户端
        /// </summary>
        public void Process()
        {
            Socket socket = ThreadSocketPairs[Thread.CurrentThread];
            try
            {
                while (true)
                {
                    //recive
                    Package pRecive = Package.Recive(socket);
                    WriteLine(pRecive);

                    //send
                    Package pSend = new Package { ServiceType = Service.ACK };
                    Package.Send(socket, pSend);
                }
            }
            //特殊错误情况：如果远程主机关闭了套接字，则Receive函数立刻返回。但由于未收到信息，所以反序列化时会报错
            catch (System.Runtime.Serialization.SerializationException) { WriteLine("远程主机已断开连接"); }
            catch (Exception e) { WriteLine(e.ToString()); }
            finally
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                WriteLine("Thread exit.");
            }
        }
    }
}
