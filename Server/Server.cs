using ClassLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using static ClassLibrary.EnDe;
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
        /// RSA私钥
        /// </summary>
        private readonly RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        /// <summary>
        /// 日志文件
        /// </summary>
        private readonly StreamWriter log;

        /// <summary>
        /// 无参构造函数，对变量进行赋值和初始化
        /// </summary>
        public Server()
        {
            //打开/创建日志文件，并使用追加模式写入
            log = new StreamWriter("..\\..\\" + DateTime.Today.ToLongDateString() + ".log", true);
            //注册处理ctrl+c事件的委托
            CancelKeyPress += new ConsoleCancelEventHandler(Server_CancelKeyPress);
            //绑定套接字
            ThreadSocketPairs = new Dictionary<Thread, Socket>();
            ipAddress = IPAddress.Parse("127.0.0.1");
            localEndPoint = new IPEndPoint(ipAddress, 11000);
            //检查公钥和私钥文件是否存在，存在则读进私钥
            try
            {
                StreamReader sr1 = new StreamReader("..\\..\\rsa_public", true);
                StreamReader sr2 = new StreamReader("..\\..\\rsa_private", true);
                rsa.FromXmlString(sr2.ReadToEnd());
                sr1.Close();
                sr2.Close();
            }
            //若文件不存在，则重新生成新的公钥和私钥对
            catch
            {
                StreamWriter sw1 = new StreamWriter("..\\..\\rsa_public", false);
                StreamWriter sw2 = new StreamWriter("..\\..\\rsa_private", false);
                sw1.Write(rsa.ToXmlString(false));
                sw2.Write(rsa.ToXmlString(true));
                sw1.Close();
                sw2.Close();
            }
        }
        /// <summary>
        /// 处理ctrl+c终端的函数
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件数据</param>
        private void Server_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            log.WriteLine($"{DateTime.Now.ToLocalTime()} 服务器退出");
            log.Close();
            foreach (Thread t in ThreadSocketPairs.Keys) { t.Abort(); }
            Thread.CurrentThread.Abort();
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
                    log.WriteLine($"{DateTime.Now.ToLocalTime()} 服务器启动成功");
                    while (true)
                    {
                        WriteLine("Waiting for a connection...");
                        Thread workingThread = new Thread(Process);
                        Socket workingSocket = listener.Accept();
                        log.WriteLine($"{DateTime.Now.ToLocalTime()} {workingSocket.RemoteEndPoint} 连接");
                        ThreadSocketPairs.Add(workingThread, workingSocket);
                        workingThread.Start();
                    }
                }
                catch (SocketException e)
                {
                    log.WriteLine($"{DateTime.Now.ToLocalTime()} {listener.LocalEndPoint} 绑定/侦听失败，服务器启动失败");
                    log.WriteLine(e.ToString());
                }
                finally { log.Close(); }
            }
        }
        /// <summary>
        /// 服务器线程函数，处理交互逻辑，一个线程对应一个客户端
        /// </summary>
        public void Process()
        {
            #region 变量声明，解决作用域的限制
            Socket socket = ThreadSocketPairs[Thread.CurrentThread];//获取线程对应的套接字
            string UserName = null;     //登陆后用来标识该线程连接对应的用户
            FileStream fs = null;       //文件流
            Package pRecive;            //接收数据包
            Package pSend;              //发送数据包
            byte[] buffer = null;       //接收字符串缓冲区
            byte[] key = null;
            int ret;                    //读取的文件字节数
            #endregion
            try
            {
                while (true)
                {
                    pRecive = Package.Recive(socket, key, rsa);//接收数据包
                    pSend = new Package();//在switch外声明，以便可以在switch块后统一发送
                    //分类处理逻辑
                    switch (pRecive.ServiceType)
                    {
                        //注册
                        case Service.Registration:
                            using (var db = new UserRegistration())
                            {
                                //密码为空，拒绝注册
                                if (Decode(pRecive.PayLoad[1]) == "")
                                {
                                    pSend.ServiceType = Service.EmptyPassword;
                                    log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} 用空密码注册");
                                }
                                else
                                {
                                    try
                                    {
                                        //用户信息写入数据库，同时告知注册成功
                                        User newUser = new User(Decode(pRecive.PayLoad[0]), Decode(pRecive.PayLoad[1]));
                                        db.UserContext.Add(newUser);
                                        db.SaveChanges();
                                        pSend.ServiceType = Service.RegistrationSuccess;
                                        log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} 以{Decode(pRecive.PayLoad[0])}注册成功");
                                    }
                                    //唯一的报错可能，用户名重复。告知客户端后拒绝注册
                                    catch
                                    {
                                        log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} 以重复用户名注册");
                                        pSend.ServiceType = Service.UserExist;
                                    }
                                }
                            }
                            break;
                        //登陆
                        case Service.Login:
                            using (var db = new UserRegistration())
                            {
                                //Linq不支持表达式查询，所以只能先把用户名和密码提取出来
                                UserName = Decode(pRecive.PayLoad[0]);
                                var pw = Decode(pRecive.PayLoad[1]).GetHashCode();

                                //查询符合的用户名与密码组合
                                var user = from u in db.UserContext
                                           where u.Name.Equals(UserName) && u.Password == pw
                                           select u;
                                //如果不存在这样的组合，则回报用户名或密码错误，若存在，则登陆成功
                                if (user.Count() == 0)
                                {
                                    log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} 密码错误");
                                    pSend.ServiceType = Service.WrongPassword;
                                }
                                else
                                {
                                    //用户存在，则加载用户对应的AES密钥
                                    log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} 以{UserName}登陆");
                                    using (var hash = new SHA384Managed())
                                        key = hash.ComputeHash(pRecive.PayLoad[1]);
                                    UserName = Decode(pRecive.PayLoad[0]);
                                    pSend.ServiceType = Service.LoginSuccess;
                                }
                            }
                            break;
                        //登出
                        case Service.Logout:
                            log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} 用户{UserName}退出登陆");
                            pSend.ServiceType = Service.ACK;
                            UserName = null;
                            key = null;
                            break;
                        //下载请求（服务器给客户端发送首份文件）
                        case Service.DownLoadSYN:
                            try
                            {
                                //打开文件，读取
                                fs = new FileStream(Decode(pRecive.PayLoad[0]), FileMode.Open, FileAccess.Read);
                                log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} {UserName} 下载{fs.Name}");
                                buffer = new byte[Size.FileSize];
                                ret = fs.Read(buffer, 0, 1);
                                //未读到文件结尾
                                if (ret != 0)
                                {
                                    pSend.ServiceType = Service.DownLoad;
                                    pSend.PayLoad.Add(buffer);
                                }
                                //读到文件结尾
                                else
                                {
                                    log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} {UserName} 下载{fs.Name}成功");
                                    fs.Close();
                                    pSend.ServiceType = Service.EOF;
                                }
                                break;
                            }
                            //告诉客户端文件不存在
                            catch (FileNotFoundException)
                            {
                                log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} {UserName} 下载{Decode(pRecive.PayLoad[0])}失败");
                                pSend.ServiceType = Service.FileNotFound;
                                break;
                            }
                        //下载中（服务器继续给客户端发送文件）
                        case Service.ACK:
                            buffer = new byte[Size.FileSize];
                            ret = fs.Read(buffer, 0, 1);
                            //未读到文件结尾
                            if (ret != 0)
                            {
                                pSend.ServiceType = Service.DownLoad;
                                pSend.PayLoad.Add(buffer);
                            }
                            //读到文件结尾
                            else
                            {
                                log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} {UserName} 下载{fs.Name}成功");
                                fs.Close();
                                pSend.ServiceType = Service.EOF;
                            }
                            break;
                        //上传请求（打开文件，准备接收）
                        case Service.UpLoadSYN:
                            fs = new FileStream(Decode(pRecive.PayLoad[0]), FileMode.OpenOrCreate, FileAccess.Write);
                            log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} {UserName} 上传{fs.Name}");
                            fs.Seek(0, SeekOrigin.End);//打开文件将指针放到结尾（断点续传使用）
                            pSend.ServiceType = Service.ACK;
                            break;
                        //上传中（继续接收文件）
                        case Service.UpLoad:
                            fs.Write(pRecive.PayLoad[0], 0, 1);
                            pSend.ServiceType = Service.ACK;
                            break;
                        //上传结束
                        case Service.EOF:
                            log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} {UserName} 上传{fs.Name}成功");
                            fs.Close();
                            break;
                    }
                    //发送回复包
                    Package.Send(socket, pSend, key, rsa);
                }
            }
            //特殊错误情况：如果远程主机关闭了套接字，则Receive函数立刻返回。但由于未收到信息，所以反序列化时会报错
            catch (System.Runtime.Serialization.SerializationException) { WriteLine("远程主机已断开连接"); }
            catch (SocketException) { WriteLine("远程主机已断开连接"); }
            catch (Exception e)
            {
                log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} {UserName} {e}");
                WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} {UserName} {e}");
            }
            finally
            {
                log.WriteLine($"{DateTime.Now.ToLocalTime()} {socket.RemoteEndPoint} 客户端关闭");
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }
    }
}
