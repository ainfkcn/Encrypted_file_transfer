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

namespace ClientGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TextBoxPassword.Text = PasswordBox.Password;
            TextBoxPassword.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBoxPassword.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window.Height = 639;
            Window.Width = 960;
            Window.Top = (SystemParameters.PrimaryScreenHeight - Window.Height) / 2;
            Window.Left = (SystemParameters.PrimaryScreenWidth - Window.Width) / 2;
            Canvas1.Visibility = Visibility.Collapsed;
            Canvas2.Visibility = Visibility.Visible;
            // Get every logical drive on the machine
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Height = 296;
            Window.Width = 436;
            Window.Top = (SystemParameters.PrimaryScreenHeight - Window.Height) / 2;
            Window.Left = (SystemParameters.PrimaryScreenWidth - Window.Width) / 2;
            Canvas1.Visibility = Visibility.Visible;
            Canvas2.Visibility = Visibility.Collapsed;
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
    }
}
