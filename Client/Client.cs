using ClassLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using static ClassLibrary.EnDe;
using static System.Console;

namespace Client
{
    public class Client
    { 
        /// <summary>
        /// 通信中使用的Tcp套接字
        /// </summary>
        private readonly Socket socket;
        /// <summary>
        /// 套接字绑定时所需的远端地址
        /// </summary>
        private readonly IPEndPoint remoteEP;
        /// <summary>
        /// AES密钥
        /// </summary>
        private byte[] key;
        /// <summary>
        /// RSA公钥
        /// </summary>
        private readonly RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

        /// <summary>
        /// 默认无参数构造函数，初始化Tcp套接字
        /// </summary>
        public Client()
        {
            //绑定套接字
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            remoteEP = new IPEndPoint(ipAddress, 11000);
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //加载公钥，如果公钥文件不存在，则报错退出
            try
            {
                StreamReader sr = new StreamReader("..\\..\\rsa_public", true);
                rsa.FromXmlString(sr.ReadToEnd());
                sr.Close();
            }
            catch (FileNotFoundException) { throw new Exception("服务器公钥丢失，请去指定位置下载服务器公钥"); }
        }
        /// <summary>
        /// 客户端启动，连接后开始进入交互函数
        /// </summary>
        public void Start()
        {
            //连接服务器，交互，关闭套接字
            try
            {
                socket.Connect(remoteEP);
                WriteLine("Socket connected to {0}", socket.RemoteEndPoint.ToString());
                Shell();
                socket.Shutdown(SocketShutdown.Both);
            }
            //出错则说明服务器未启动
            catch (SocketException) { WriteLine("远程服务器未启动"); }
            //释放套接字
            finally { socket.Close(); }
        }
        /// <summary>
        /// 构想中的交互函数，无参型；有参型用来做shell
        /// </summary>
        public void Shell()
        {
            #region 变量声明，解决作用域的限制
            Package pSend;              //发送数据包
            Package pRecive;            //接收数据包
            string UserName = null;     //用户名
            string op;                  //用户操作
            string FileName = null;     //文件名
            string LocalDirectory = Directory.GetCurrentDirectory();//本地路径
            string RemoteDirectory;     //远程路径
            bool Login = false;         //是否登录
            bool DorU = false;          //是否在上传或下载中
            FileStream fs = null;       //文件流
            #endregion
            try
            {
                while (true)
                {
                    //未登陆时的提示符和操作逻辑
                    if (!Login && !DorU)
                    {
                        //用户操作选项和提示符
                        WriteLine("1.Login");
                        WriteLine("9.Registration");
                        WriteLine("0.Exit");
                        Write(">");
                        op = ReadLine().ToLower();//读入操作指令
                        //登陆部分
                        if (op.Equals("l") || op.Equals("log") || op.Equals("1"))
                        {
                            pSend = new Package { ServiceType = Service.Login };//数据包构造
                            Write("用户名:");
                            UserName = ReadLine();
                            pSend.PayLoad.Add(Encode(UserName));//用户名
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
                                    using (var hash = new SHA384Managed())
                                        key = hash.ComputeHash(Encode(pw));
                                    pSend.PayLoad.Add(Encode(pw));
                                    break;
                                }
                                else
                                    password.Add(temp);
                            }
                            WriteLine();
                        }
                        //注册部分
                        else if (op.Equals("r") || op.Equals("reg") || op.Equals("9"))
                        {
                            pSend = new Package { ServiceType = Service.Registration };//数据包构造
                            Write("用户名:"); pSend.PayLoad.Add(Encode(ReadLine()));//用户名
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
                                pSend.PayLoad.Add(Encode(pw1));
                                WriteLine();
                            }
                            else
                                WriteLine("\n两次密码不匹配");
                        }
                        //退出
                        else if (op.Equals("e") || op.Equals("exit") || op.Equals("0")) break;
                        //假如输入了其他乱七八糟的指令
                        else continue;
                        //发送数据包
                        Package.Send(socket, pSend, key, rsa);
                    }
                    //登陆后的提示符和操作逻辑
                    else if (Login && !DorU)
                    {
                        //用户操作提示符
                        WriteLine("1.DownLoad");
                        WriteLine("2.UpLoad");
                        WriteLine("3.LocalDirectory");
                        WriteLine("4.RemoteDirectory");
                        WriteLine("0.Exit");
                        Write(UserName + ">");
                        op = ReadLine().ToLower();//读入操作指令
                        //下载
                        if (op.Equals("d") || op.Contains("down") || op.Equals("1"))
                        {
                            Write("FileName:");
                            FileName = ReadLine();//读取要下载的文件名
                            if (FileName.Contains("rsa_private"))
                            {
                                WriteLine("不允许下载私钥");
                                continue;
                            }
                            DorU = true;
                            fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                            fs.Seek(0, SeekOrigin.End);//打开文件并将光标设置到末尾（断点续传需要）
                            pSend = new Package { ServiceType = Service.DownLoadSYN };
                            pSend.PayLoad.Add(Encode(FileName));//将文件名发送给服务端，让服务器寻找文件
                            DriveInfo driveInfo = new DriveInfo(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()));
                            pSend.PayLoad.Add(Encode(driveInfo.AvailableFreeSpace.ToString()));
                        }
                        //上传
                        else if (op.Equals("u") || op.Contains("up") || op.Equals("2"))
                        {
                            DorU = true;//设置上传下载中标志位
                            Write("FileName:");
                            FileName = ReadLine();//读取要上传的文件名
                            //打开文件
                            try { fs = new FileStream(LocalDirectory + @"\" + FileName, FileMode.Open, FileAccess.Read); }
                            //文件不存在，解除上传下载模式
                            catch (FileNotFoundException)
                            {
                                DorU = false;//恢复上传下载标志位
                                FileName = null;
                                WriteLine("本地不存在此文件，请检查拼写");
                                continue;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                DorU = false;//恢复上传下载标志位
                                FileName = null;
                                WriteLine("不能上传文件夹");
                                continue;
                            }
                            pSend = new Package { ServiceType = Service.UpLoadSYN };
                            pSend.PayLoad.Add(Encode(FileName));//将文件名发送给服务器
                            FileInfo fi = new FileInfo(fs.Name);
                            pSend.PayLoad.Add(Encode(fi.Length.ToString()));
                        }
                        //浏览本地目录
                        else if (op.Equals("l") || op.Contains("local") || op.Equals("3"))
                        {
                            while (true)
                            {
                                LocalDirectory = Directory.GetCurrentDirectory();//获取当前目录
                                string dir = CommandLine.Dir(LocalDirectory);//显示目录内容
                                Write("本地目录："); WriteLine(dir);
                                WriteLine("按回车键返回，或使用cd命令切换目录");
                                op = ReadLine();
                                if (op.Equals(""))//无输入退出
                                    break;
                                else if (op.StartsWith("cd "))//切换目录
                                {
                                    op = op.Remove(0, 3);
                                    try { CommandLine.Cd(op); }
                                    catch { WriteLine("该目录不存在"); }
                                }
                                WriteLine();
                            }
                            continue;
                        }
                        //浏览远程目录
                        else if (op.Equals("r") || op.Contains("remote") || op.Equals("4"))
                        {
                            while (true)
                            {
                                //查询远程目录
                                pSend = new Package { ServiceType = Service.Directory };
                                Package.Send(socket, pSend, key, rsa);
                                pRecive = Package.Recive(socket, key, rsa);
                                //更新本地记录
                                if (pRecive.ServiceType == Service.Directory)
                                {
                                    RemoteDirectory = Decode(pRecive.PayLoad[0]);
                                    Write("远程目录："); WriteLine(Decode(pRecive.PayLoad[1]));
                                }
                                WriteLine("按回车键返回，或使用cd命令切换目录");
                                op = ReadLine();
                                if (op.Equals(""))//无输入退出
                                    break;
                                else if (op.StartsWith("cd "))//切换目录
                                {
                                    op = op.Remove(0, 3);
                                    //构造数据包
                                    pSend = new Package { ServiceType = Service.ChangeDirectory };
                                    pSend.PayLoad.Add(Encode(op));
                                    //发送
                                    Package.Send(socket, pSend, key, rsa);
                                    pRecive = Package.Recive(socket, key, rsa);
                                    //服务端报错：目录不存在
                                    if (pRecive.ServiceType == Service.DirectoryNotFound)
                                        WriteLine("远端不存在此目录");
                                }
                                WriteLine();
                            }
                            continue;
                        }
                        //退出
                        else if (op.Equals("e") || op.Contains("exit") || op.Equals("0"))
                        {
                            pSend = new Package { ServiceType = Service.Logout };
                            Login = false;
                            UserName = null;
                            key = null;
                            WriteLine("已退出登录");
                        }
                        //如果输入了其他乱七八糟的指令
                        else continue;
                        //发送数据包
                        Package.Send(socket, pSend, key, rsa);
                    }
                    //接收数据包
                    pRecive = Package.Recive(socket, key, rsa);
                    //处理逻辑
                    switch (pRecive.ServiceType)
                    {
                        #region 注册
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
                        #endregion
                        #region 登录
                        //登陆时密码或用户名错误
                        case Service.WrongPassword:
                            WriteLine("用户名或密码错误");
                            UserName = null;
                            key = null;
                            break;
                        //登录成功
                        case Service.LoginSuccess:
                            WriteLine("登陆成功");
                            Login = true;
                            break;
                        #endregion
                        #region 下载
                        //下载中（将接收到的数据写入文件）
                        case Service.DownLoad:
                            fs.Write(pRecive.PayLoad[0], 0, Size.FileSize);
                            pSend = new Package { ServiceType = Service.ACK };
                            Package.Send(socket, pSend, key, rsa);
                            break;
                        //下载结束
                        case Service.EOF:
                            DorU = false;
                            WriteLine("下载成功");
                            fs.Close();
                            break;
                        //服务器未找到要下载的文件
                        case Service.FileNotFound:
                            fs.Close();
                            File.Delete(FileName);//删除创建的空文件
                            FileName = null;
                            DorU = false;
                            WriteLine("远程不存在此文件或是目录，请确定文件存在且不是目录");
                            break;
                        //客户磁盘空间不足
                        case Service.ClientNoEnoughSpace:
                            fs.Close();
                            File.Delete(FileName);//删除创建的空文件
                            FileName = null;
                            DorU = false;
                            WriteLine("磁盘空间不足");
                            break;
                        //试图下载私钥
                        case Service.TryDownloadPrivate:
                            fs.Close();
                            File.Delete(FileName);//删除创建的空文件
                            FileName = null;
                            DorU = false;
                            WriteLine("不允许下载私钥");
                            break;
                        #endregion
                        #region 上传
                        //上传中（读取文件发送给服务器端）
                        case Service.ACK:
                            if (Login)
                            {
                                byte[] buffer = new byte[Size.FileSize];
                                //未到文件末尾
                                if (fs.Read(buffer, 0, Size.FileSize) != 0)
                                {
                                    pSend = new Package { ServiceType = Service.UpLoad };
                                    pSend.PayLoad.Add(buffer);
                                }
                                //到了文件末尾
                                else
                                {
                                    DorU = false;
                                    WriteLine("上传完成");
                                    fs.Close();
                                    pSend = new Package { ServiceType = Service.EOF };
                                }
                                Package.Send(socket, pSend, key, rsa);
                            }
                            break;
                        //服务器空间不足
                        case Service.ServerNoEnoughSpace:
                            DorU = false;
                            WriteLine("上传失败，服务器空间不足");
                            fs.Close();
                            break;
                            #endregion
                    }
                }
            }
            //特殊错误情况：如果远程主机关闭了套接字，则Receive函数立刻返回。但由于未收到信息，所以反序列化时会报错
            catch (System.Runtime.Serialization.SerializationException) { WriteLine("远程主机已断开连接"); }
            catch (Exception e) { WriteLine(e.ToString()); }
        }
    }
}
