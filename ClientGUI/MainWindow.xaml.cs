using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Diagnostics;
using ClassLibrary;
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
        Package pSend;                          //发送数据包
        string UserName = null;                 //用户名
        Package pRecive;                        //接收数据包

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
            Window.Height = 296;
            Window.Width = 436;
            Window.Top = (SystemParameters.PrimaryScreenHeight - Window.Height) / 2;
            Window.Left = (SystemParameters.PrimaryScreenWidth - Window.Width) / 2;
            Canvas1.Visibility = Visibility.Visible;
            Canvas2.Visibility = Visibility.Collapsed;
            //连接服务器
            try
            {
                socket.Connect(remoteEP);
                MessageBox.Show($"Socket connected to {socket.RemoteEndPoint}");
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

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            //发送登陆数据包
            pSend = new Package { ServiceType = Service.Login };//数据包构造
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
            switch (pRecive.ServiceType)
            {
                case Service.WrongPassword:
                    MessageBox.Show("用户名或密码错误");
                    UserName = string.Empty;
                    key = null;
                    //TextBoxUserName.Text = string.Empty;
                    PasswordBox.Password = string.Empty;
                    TextBoxPassword.Text = string.Empty;
                    break;
                case Service.LoginSuccess:
                    MessageBox.Show("登陆成功");
                    //窗口属性设置
                    Window.Height = 639;
                    Window.Width = 960;
                    Window.Top = (SystemParameters.PrimaryScreenHeight - Window.Height) / 2;
                    Window.Left = (SystemParameters.PrimaryScreenWidth - Window.Width) / 2;
                    Canvas1.Visibility = Visibility.Collapsed;
                    Canvas2.Visibility = Visibility.Visible;
                    //目录树初始化
                    foreach (var drive in Directory.GetLogicalDrives())
                    {
                        // Create a new item for it
                        var item = new TreeViewItem()
                        {
                            // Set the header
                            Header = drive,
                            // And the full path
                            Tag = drive
                        };
                        // Add a dummy item
                        item.Items.Add(null);
                        // Listen out for item being expanded
                        item.Expanded += Folder_Expanded;
                        // Add it to the main tree-view
                        LocalView.Items.Add(item);
                    }
                    foreach (var drive in Directory.GetLogicalDrives())
                    {
                        // Create a new item for it
                        var item = new TreeViewItem()
                        {
                            // Set the header
                            Header = drive,
                            // And the full path
                            Tag = drive
                        };
                        // Add a dummy item
                        item.Items.Add(null);
                        // Listen out for item being expanded
                        item.Expanded += Folder_Expanded;
                        // Add it to the main tree-view
                        RemoteView.Items.Add(item);
                    }
                    break;
            }
        }

        private void ButtonRegistration_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            Window.Height = 296;
            Window.Width = 436;
            Window.Top = (SystemParameters.PrimaryScreenHeight - Window.Height) / 2;
            Window.Left = (SystemParameters.PrimaryScreenWidth - Window.Width) / 2;
            Canvas1.Visibility = Visibility.Visible;
            Canvas2.Visibility = Visibility.Collapsed;
            LocalView.Items.Clear();
            RemoteView.Items.Clear();
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            #region Initial Checks
            TreeViewItem item = sender as TreeViewItem;
            // If the item only contains the dummy data
            if (item.Items.Count != 1 || item.Items[0] != null)
                return;
            // Clear dummy data
            item.Items.Clear();
            // Get full path
            string fullPath = item.Tag as string;
            #endregion

            #region Get Folders
            // Create a blank list for directories
            List<string> directories = new List<string>();
            // Try and get directories from the folder
            // ignoring any issues doing so
            try
            {
                var dirs = Directory.GetDirectories(fullPath);
                if (dirs.Length > 0)
                    directories.AddRange(dirs);
            }
            catch { }
            // For each directory...
            directories.ForEach(directoryPath =>
            {
                // Create directory item
                var subItem = new TreeViewItem()
                {
                    // Set header as folder name
                    Header = GetFileFolderName(directoryPath),
                    // And tag as full path
                    Tag = directoryPath
                };
                // Add dummy item so we can expand folder
                subItem.Items.Add(null);
                // Handle expanding
                subItem.Expanded += Folder_Expanded;
                // Add this item to the parent
                item.Items.Add(subItem);
            });
            #endregion

            #region Get Files
            // Create a blank list for files
            var files = new List<string>();
            // Try and get files from the folder
            // ignoring any issues doing so
            try
            {
                var fs = Directory.GetFiles(fullPath);
                if (fs.Length > 0)
                    files.AddRange(fs);
            }
            catch { }
            // For each file...
            files.ForEach(filePath =>
            {
                // Create file item
                var subItem = new TreeViewItem()
                {
                    // Set header as file name
                    Header = GetFileFolderName(filePath),
                    // And tag as full path
                    Tag = filePath
                };
                // Add this item to the parent
                item.Items.Add(subItem);
            });
            #endregion
        }

        public static string GetFileFolderName(string path)
        {
            // If we have no path, return empty
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            // Make all slashes back slashes
            var normalizedPath = path.Replace('/', '\\');
            // Find the last backslash in the path
            var lastIndex = normalizedPath.LastIndexOf('\\');
            // If we don't find a backslash, return the path itself
            if (lastIndex <= 0)
                return path;
            // Return the name after the last back slash
            return path.Substring(lastIndex + 1);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //释放套接字
            try { socket.Shutdown(SocketShutdown.Both); }
            catch { }
            finally { socket.Close(); }
        }
    }
}
