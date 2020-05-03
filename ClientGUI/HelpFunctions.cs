using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ClientGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 调整窗口至登录窗口（居中，小窗，画布1）
        /// </summary>
        private void AdjustWindowToScreen1()
        {
            Window.Height = 296;
            Window.Width = 436;
            Window.Top = (SystemParameters.PrimaryScreenHeight - Window.Height) / 2;
            Window.Left = (SystemParameters.PrimaryScreenWidth - Window.Width) / 2;
            Canvas1.Visibility = Visibility.Visible;
            Canvas2.Visibility = Visibility.Collapsed;
            InfoClear(true);
        }
        /// <summary>
        /// 调整窗口至登陆后窗口（居中，大窗，画布2）
        /// </summary>
        private void AdjustWindowToScreen2()
        {
            Window.Height = 639;
            Window.Width = 960;
            Window.Top = (SystemParameters.PrimaryScreenHeight - Window.Height) / 2;
            Window.Left = (SystemParameters.PrimaryScreenWidth - Window.Width) / 2;
            Canvas1.Visibility = Visibility.Collapsed;
            Canvas2.Visibility = Visibility.Visible;
            InfoClear(true);
        }
        /// <summary>
        /// 清除各个窗口中的信息
        /// </summary>
        /// <param name="clearUserName"></param>
        private void InfoClear(bool clearUserName = false)
        {
            if (clearUserName)
                TextBoxUserName.Text = string.Empty;
            PasswordBox.Password = string.Empty;
            TextBoxPassword.Text = string.Empty;
            TextBlockUser.Text = string.Empty;
            LocalView.Items.Clear();
            RemoteView.Items.Clear();
        }
        /// <summary>
        /// 文件树展开时的事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// 从完整路径中获取文件夹名称的函数
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetFileFolderName(string path)
        {
            // If we have no path, return empty
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            // Find the last backslash in the path
            var lastIndex = path.LastIndexOf('\\');
            // If we don't find a backslash, return the path itself
            if (lastIndex <= 0)
                return path;
            // Return the name after the last back slash
            return path.Substring(lastIndex + 1);
        }
        /// <summary>
        /// 构建本地目录树并展开到运行路径
        /// </summary>
        private void LocalViewInitialize()
        {
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
            //迭代器拼接路径展开树状图到当前运行路径
            var cwd = Directory.GetCurrentDirectory().Split('\\').GetEnumerator();
            string path = string.Empty;
            ItemCollection items = LocalView.Items;
            while (cwd.MoveNext())
            {
                //拼接路径
                path += cwd.Current as string;
                //寻找路径匹配的项目
                foreach (TreeViewItem item in items)
                    if (((string)item.Tag).Trim('\\') == path)
                    {
                        item.IsSelected = true; //选中
                        item.IsExpanded = true; //展开
                        items = item.Items;     //将迭代的项目换为此项目
                        break;
                    }
                path += "\\";
            }
        }
        /// <summary>
        /// 构建“远程”目录树并展开到运行路径
        /// </summary>
        /// <remarks>
        /// （使用了作弊手段，利用的是服务器和客户端都在本机上,未经过网络通信协议）
        /// </remarks>
        private void RemoteViewInitialize()
        {
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
            Process[] processes = Process.GetProcessesByName("Server");
            var temp = processes[0].MainModule.FileName;
            var cwd = temp.Substring(0, temp.LastIndexOf('\\')).Split('\\').GetEnumerator();
            string path = string.Empty;
            ItemCollection items = RemoteView.Items;
            while (cwd.MoveNext())
            {
                //拼接路径
                path += cwd.Current as string;
                //寻找路径匹配的项目
                foreach (TreeViewItem item in items)
                    if (((string)item.Tag).Trim('\\') == path)
                    {
                        item.IsSelected = true; //选中
                        item.IsExpanded = true; //展开
                        items = item.Items;     //将迭代的项目换为此项目
                        break;
                    }
                path += "\\";
            }
        }
    }
}