using ClassLibrary;
using System;
using System.Collections.Generic;
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
                    #region 用户交互逻辑
                    //用户操作选项和提示符
                    WriteLine("1.Login");
                    WriteLine("0.Registration");
                    Write("Client>");

                    string op = ReadLine().ToLower();//读入操作指令

                    Package pSend = new Package();//在判断体外声明，便于判断中处理后直接发出

                    //注册部分
                    if (op.Contains("r") || op.Contains("reg") || op.Equals("0"))
                    {
                        pSend.ServiceType = Service.Registration;//数据包构造

                        Write("用户名:"); pSend.PayLoad.Add(ReadLine());//用户名

                        //第一次输入密码（不回显）
                        Write("密码:");
                        string pw1;
                        List<char> password = new List<char>();
                        while (true)
                        {
                            char temp = ReadKey(true).KeyChar;//读取输入

                            //如果是回车键跳出，不是则将字符附加到串的尾部
                            if (temp == '\r')
                            {
                                pw1 = new string(password.ToArray());
                                break;
                            }
                            else
                                password.Add(temp);
                        }

                        //第二次确认密码（不回显）
                        Write("\n再次输入密码:");
                        string pw2;
                        password.Clear();
                        while (true)
                        {
                            char temp = ReadKey(true).KeyChar;//读取输入

                            //如果是回车键跳出，不是则将字符附加到串的尾部
                            if (temp == '\r')
                            {
                                pw2 = new string(password.ToArray());
                                break;
                            }
                            else
                                password.Add(temp);
                        }

                        //检测两次输入的密码是否一致，一致的通过，进行注册。不一致则直接返回
                        if (pw1.Equals(pw2))
                        {
                            pSend.PayLoad.Add(pw1);
                            WriteLine();
                        }
                        else
                            WriteLine("\n两次密码不匹配");
                    }
                    //登陆部分
                    if (op.Contains("l") || op.Contains("log") || op.Equals("1"))
                    {
                        pSend.ServiceType = Service.Login;//数据包构造

                        Write("用户名:"); pSend.PayLoad.Add(ReadLine());//用户名

                        //密码（不回显）
                        Write("密码:");
                        string pw;
                        List<char> password = new List<char>();
                        while (true)
                        {
                            char temp = ReadKey(true).KeyChar;//读取输入

                            //如果是回车键跳出，不是则将字符附加到串的尾部
                            if (temp == '\r')
                            {
                                pw = new string(password.ToArray());
                                pSend.PayLoad.Add(pw);
                                break;
                            }
                            else
                                password.Add(temp);
                        }
                        WriteLine();
                    }


                    //发送
                    Package.Send(socket, pSend);
                    #endregion


                    #region 返回包自动处理逻辑
                    Package pRecive = Package.Recive(socket);
                    WriteLine(pRecive);
                    switch (pRecive.ServiceType)
                    {
                        //注册成功
                        case Service.RegistrationSuccess:
                            WriteLine("注册成功，请记好用户名和密码");
                            break;
                        //注册密码为空
                        case Service.EmptyPassword:
                            WriteLine("注册失败，密码不能为空");
                            break;
                        //注册用户名已存在
                        case Service.UserExist:
                            WriteLine("用户名已存在");
                            break;
                        //登陆时密码或用户名错误
                        case Service.WrongPassword:
                            WriteLine("用户名或密码错误");
                            break;
                    }
                    #endregion
                }
            }
            //特殊错误情况：如果远程主机关闭了套接字，则Receive函数立刻返回。但由于未收到信息，所以反序列化时会报错
            catch (System.Runtime.Serialization.SerializationException) { WriteLine("远程主机已断开连接"); }
            catch (Exception e) { WriteLine(e.ToString()); }
        }
    }
}
