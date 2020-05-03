using ClassLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using static ClassLibrary.EnDe;

namespace ClientGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Socket socket;         //通信中使用的Tcp套接字
        private readonly IPEndPoint remoteEP;   //套接字绑定时所需的远端地址
        private byte[] key;                     //AES密钥
        private readonly RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();//RSA公钥
        private Package pSend;                  //发送数据包
        public string UserName = null;          //用户名
        private Package pRecive;                //接收数据包
        /// <summary>
        /// 构造函数，初始化变量和窗口
        /// </summary>
        public MainWindow()
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
            catch (FileNotFoundException)
            {
                MessageBox.Show("服务器公钥丢失，请去指定位置下载服务器公钥");
                Process.GetCurrentProcess().Kill();
            }
            //初始化窗口元素
            InitializeComponent();
        }
        /// <summary>
        /// 窗口启动时进行socket的链接与窗口的一些设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //窗口属性设置
            AdjustWindowToScreen1();
            //连接服务器
            try
            {
                socket.Connect(remoteEP);
                //MessageBox.Show($"Socket connected to {socket.RemoteEndPoint}");
            }
            //出错则说明服务器未启动，结束程序
            catch (SocketException)
            {
                MessageBox.Show("远程服务器未启动");
                Window.Close();
                Process.GetCurrentProcess().Kill();
            }
        }
        /// <summary>
        /// 显示密码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TextBoxPassword.Text = PasswordBox.Password;
            TextBoxPassword.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// 取消显示密码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBoxPassword.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// 按下登录按钮后的事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            //发送登陆数据包
            pSend = new Package { ServiceType = Service.Login };
            //填充用户名
            UserName = TextBoxUserName.Text;
            pSend.PayLoad.Add(Encode(UserName));
            //生成AES密钥
            using (var hash = new SHA384Managed())
                key = hash.ComputeHash(Encode(PasswordBox.Password));
            //填充登录口令
            pSend.PayLoad.Add(Encode(PasswordBox.Password));
            //发送数据包
            Package.Send(socket, pSend, key, rsa);
            //接收数据包
            pRecive = Package.Recive(socket, key, rsa);
            //分类处理
            switch (pRecive.ServiceType)
            {
                case Service.WrongPassword:
                    MessageBox.Show("用户名或密码错误");
                    UserName = string.Empty;
                    key = null;
                    InfoClear();
                    break;
                case Service.LoginSuccess:
                    //MessageBox.Show("登陆成功");
                    //窗口属性设置
                    AdjustWindowToScreen2();
                    TextBlockUser.Text = "欢迎：" + UserName;
                    //本地目录初始化
                    LocalViewInitialize();
                    //远程目录初始化
                    RemoteViewInitialize();
                    break;
            }
        }
        /// <summary>
        /// 按下注册按钮后的事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonRegistration_Click(object sender, RoutedEventArgs e)
        {
            //发送登陆数据包
            pSend = new Package { ServiceType = Service.Registration };
            //填充用户名
            UserName = TextBoxUserName.Text;
            pSend.PayLoad.Add(Encode(UserName));
            //填充登录口令
            pSend.PayLoad.Add(Encode(PasswordBox.Password));
            //发送数据包
            Package.Send(socket, pSend, key, rsa);
            //接收数据包
            pRecive = Package.Recive(socket, key, rsa);
            //分类处理
            switch (pRecive.ServiceType)
            {
                case Service.RegistrationSuccess:
                    MessageBox.Show("注册成功，请记好用户名和密码");
                    break;
                case Service.EmptyPassword:
                    MessageBox.Show("注册失败，密码不能为空");
                    break;
                case Service.UserExist:
                    MessageBox.Show("用户名已存在");
                    break;
            }
        }
        /// <summary>
        /// 按下注销按钮后的事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            //调整窗口
            AdjustWindowToScreen1();
            //退出登录，抹除信息
            pSend = new Package { ServiceType = Service.Logout };
            Package.Send(socket, pSend, key, rsa);
            Package.Recive(socket, key, rsa);
            UserName = null;
            key = null;
        }
        /// <summary>
        /// 窗口关闭时的事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            //释放套接字
            try { socket.Shutdown(SocketShutdown.Both); }
            catch { }
            //关闭套接字
            finally { socket.Close(); }
        }
        /// <summary>
        /// 上传按钮的事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            //冻结窗口
            Window.IsEnabled = false;
            string FileName = ((TreeViewItem)LocalView.SelectedItem).Tag as string;
            FileStream fs;
            //检查文件属性是否正确
            try { fs = new FileStream(FileName, FileMode.Open, FileAccess.Read); }
            catch (FileNotFoundException) { MessageBox.Show("文件不存在"); Window.IsEnabled = true; return; }
            catch (UnauthorizedAccessException) { MessageBox.Show("不能上传文件夹"); Window.IsEnabled = true; return; }
            //构造通信包
            pSend = new Package { ServiceType = Service.UpLoadSYN };
            FileName = FileName.Substring(FileName.LastIndexOf('\\') + 1);
            pSend.PayLoad.Add(Encode(FileName));//将文件名发送给服务器
            FileInfo fi = new FileInfo(fs.Name);
            pSend.PayLoad.Add(Encode(fi.Length.ToString()));
            Package.Send(socket, pSend, key, rsa);
            while (true)
            {
                pRecive = Package.Recive(socket, key, rsa);
                switch (pRecive.ServiceType)
                {
                    //上传中（读取文件发送给服务器端）
                    case Service.ACK:
                        byte[] buffer = new byte[ClassLibrary.Size.FileSize];
                        //未到文件末尾
                        if (fs.Read(buffer, 0, ClassLibrary.Size.FileSize) != 0)
                        {
                            pSend = new Package { ServiceType = Service.UpLoad };
                            pSend.PayLoad.Add(buffer);
                        }
                        //到了文件末尾
                        else
                        {
                            Window.IsEnabled = true;
                            MessageBox.Show("上传完成");
                            fs.Close();
                            pSend = new Package { ServiceType = Service.EOF };
                            return;
                        }
                        Package.Send(socket, pSend, key, rsa);
                        break;
                    //服务器空间不足
                    case Service.ServerNoEnoughSpace:
                        Window.IsEnabled = true;
                        MessageBox.Show("上传失败，服务器空间不足");
                        fs.Close();
                        return;
                }
            }
        }
        /// <summary>
        /// 下载按钮的事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            //冻结窗口
            Window.IsEnabled = false;
            string FileName = ((TreeViewItem)RemoteView.SelectedItem).Tag as string;
            FileStream fs;
            //检查文件属性是否正确
            if (FileName.Contains("rsa_private"))
            {
                MessageBox.Show("不允许下载私钥");
                Window.IsEnabled = true;
                return;
            }
            //构造通信包
            pSend = new Package { ServiceType = Service.DownLoadSYN };
            pSend.PayLoad.Add(Encode(FileName));//将文件名发送给服务端，让服务器寻找文件
            DriveInfo driveInfo = new DriveInfo(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()));
            pSend.PayLoad.Add(Encode(driveInfo.AvailableFreeSpace.ToString()));
            FileName = FileName.Substring(FileName.LastIndexOf('\\') + 1);
            fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Seek(0, SeekOrigin.End);//打开文件并将光标设置到末尾（断点续传需要）
            Package.Send(socket, pSend, key, rsa);
            while (true)
            {
                pRecive = Package.Recive(socket, key, rsa);
                switch (pRecive.ServiceType)
                {
                    #region 下载
                    //下载中（将接收到的数据写入文件）
                    case Service.DownLoad:
                        fs.Write(pRecive.PayLoad[0], 0, ClassLibrary.Size.FileSize);
                        pSend = new Package { ServiceType = Service.ACK };
                        Package.Send(socket, pSend, key, rsa);
                        break;
                    //下载结束
                    case Service.EOF:
                        Window.IsEnabled = true;
                        MessageBox.Show("下载成功");
                        fs.Close();
                        return;
                    //服务器未找到要下载的文件
                    case Service.FileNotFound:
                        Window.IsEnabled = true;
                        MessageBox.Show("远程不存在此文件或是目录，请确定文件存在且不是目录");
                        fs.Close();
                        File.Delete(FileName);//删除创建的空文件
                        return;
                    //客户磁盘空间不足
                    case Service.ClientNoEnoughSpace:
                        Window.IsEnabled = true;
                        MessageBox.Show("磁盘空间不足");
                        fs.Close();
                        File.Delete(FileName);//删除创建的空文件
                        break;
                    //试图下载私钥
                    case Service.TryDownloadPrivate:
                        Window.IsEnabled = true;
                        MessageBox.Show("不允许下载私钥");
                        fs.Close();
                        File.Delete(FileName);//删除创建的空文件
                        break;
                        #endregion
                }
            }
        }
    }
}
