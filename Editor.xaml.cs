using AdonisUI.Controls;
using MinecraftResourcePack_Builder.lib;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace MinecraftResourcePack_Builder
{
    /// <summary>
    /// Editor.xaml 的交互逻辑
    /// </summary>
    public partial class Editor : AdonisWindow
    {

        private FileSystemWatcher _watcher;
        public string ProjectName { get; private set; }
        public string ProjectPath { get; private set; }
        public int OpenType { get; private set; }
        Tools tools = new Tools();
        public ObservableCollection<FolderItem> Folders { get; set; }

        public Editor(string projectName = null, int openType = 0, string projectPath = null)
        {
            ProjectName = projectName;
            ProjectPath = projectPath;
            OpenType = openType;

            InitializeComponent();

            if (openType == 1)
            {
                DockPanel.Children.Remove(ButtonPopup);
            }
            Folders = new ObservableCollection<FolderItem>(); // 初始化 Folders
            FolderItemsControl.ItemsSource = Folders;
            DataContext = this;
            LoadFoldersAndImages();
            InitializeFileSystemWatcher();

            // 为窗口添加滚轮事件处理
            this.PreviewMouseWheel += (s, e) =>
            {
                if (!e.Handled)
                {
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                    eventArg.Source = e.Source;
                    ScrollViewer.RaiseEvent(eventArg); // 将事件重定向到ScrollViewer
                }
            };

            this.Closing += delegate
            {
                Application.Current.Shutdown();
            };
        }

        private void LoadFoldersAndImages()
        {
            // Assume we show a progress bar
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 0;

            var progress = new Progress<int>(value =>
            {
                ProgressBar.Value = value;
            });

            Task.Run(async() => await LoadData(progress)).ContinueWith(t =>
             {
                 // Hide progress bar when done
                 ProgressBar.Visibility = Visibility.Hidden;
                 LoadingOverlay.Visibility = Visibility.Hidden;

                 EditorMain.Children.Remove(LoadingOverlay);
             }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task LoadData(IProgress<int> progress)
        {
            try
            {
                string[] folderPaths = ExtractFolders(tools.ProjectPath(ProjectName, @"src\assets\minecraft\textures"));
                if (folderPaths == null || folderPaths.Length == 0)
                {
                    return;
                }

                var total = folderPaths.Length;
                var currentProgress = 0;

                foreach (var folderPath in folderPaths)
                {
                    if (string.IsNullOrEmpty(folderPath))
                        continue;

                    var folderItem = new FolderItem(folderPath);
                    var imagePaths = ExtractImagesFromFolder(folderPath);

                    if (imagePaths == null)
                        continue;

                    foreach (var imagePath in imagePaths)
                    {
                        if (string.IsNullOrEmpty(imagePath))
                            continue;

                        var imageItem = new ImageItem(imagePath, folderItem);
                        LoadImage(imageItem); // LoadImage now directly works with the file system
                        folderItem.Images.Add(imageItem);

                        currentProgress++;
                        progress?.Report((int)((double)currentProgress / total * 100));
                    }

                    Application.Current.Dispatcher.Invoke(() => Folders.Add(folderItem));
                }
            }
            catch (TaskCanceledException)
            {
                // 任务已取消的相关处理
                MessageBox.Show("The task has been cancelled.");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("An error occurred: " + ex.Message));
            }
        }

        private string[] ExtractFolders(string directoryPath)
        {
            var directories = Directory.GetDirectories(directoryPath);
            return directories;
        }

        private string[] ExtractImagesFromFolder(string folderPath)
        {
            var files = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly);
            return files;
        }

        private void LoadImage(ImageItem imageItem)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = new Uri(imageItem.ImagePath, UriKind.Absolute);
                bitmapImage.EndInit();

                if (bitmapImage.CanFreeze)
                {
                    bitmapImage.Freeze(); // 确保跨线程访问
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    imageItem.ImageSource = bitmapImage;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Failed to load image: " + ex.Message));
            }
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is ImageItem imageItem)
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "PNG贴图文件 (*.png)|*.png",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    var sourceImagePath = openFileDialog.FileName;
                    // 假设 targetFolderPath 是您希望复制到的目标文件夹
                    // 检查文件夹是否存在
                    if (!tools.Isfolder(tools.ProjectPath(ProjectName, $@"src\assets\minecraft\textures\{imageItem.ParentFolder.FolderName}")))
                    {
                        Directory.CreateDirectory(tools.ProjectPath(ProjectName, $@"src\assets\minecraft\textures\{imageItem.ParentFolder.FolderName}"));
                    }
                    var targetFolderPath = tools.ProjectPath(ProjectName, $@"src\assets\minecraft\textures\{imageItem.ParentFolder.FolderName}"); // 请替换为您的目标文件夹路径
                    var targetImagePath = Path.Combine(targetFolderPath, imageItem.ImageName);
                    try
                    {
                        // 复制图片到指定文件夹，保持原文件名
                        File.Copy(sourceImagePath, targetImagePath, true);

                        // 更新ImageItem以反映新的图片位置
                        UpdateImageItem(imageItem, targetImagePath);

                        MessageBox.Show("添加成功", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"失败原因： {ex.Message}", "添加失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void UpdateImageItem(ImageItem imageItem, string imagePath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 更新ImagePath以指向新位置，这是可选的，取决于您是否希望在ImageItem对象中保留新路径
                imageItem.ImagePath = imagePath;
                imageItem.ImageSource = new BitmapImage(new Uri(imagePath));
            });
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            tools.BuildProject(ProjectName);
        }

        private void InitializeFileSystemWatcher()
        {
            try
            {
                // 设置 FileSystemWatcher来监视文本文件的变化
                _watcher = new FileSystemWatcher(tools.ProjectPath(ProjectName, @"src\assets\minecraft\textures"))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    Filter = "*.png",
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                // 注册事件处理函数
                _watcher.Changed += OnChanged;
                _watcher.Created += OnChanged;
                _watcher.Deleted += OnChanged;
                _watcher.Renamed += OnRenamed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"监听错误，请重启程序 - {ex.ToString()}");
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // 调用UI线程来更新UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 这里调用你的UI更新逻辑
                UpdateUI();
            });
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 这里调用你的UI更新逻辑
                UpdateUI();
            });
        }

        // 你可能需要编写这个函数来更新UI元素
        private void UpdateUI()
        {
            // 假设这个方法被调用来处理创建、变更或删除事件
            string[] updatedFolderPaths = ExtractFolders(tools.ProjectPath(ProjectName, @"src\assets\minecraft\textures"));

            foreach (var updatedFolderPath in updatedFolderPaths)
            {
                string folderName = Path.GetFileName(updatedFolderPath);

                // 检查ObservableCollection中是否已经存在这个folderName
                var existingFolderItem = Folders.FirstOrDefault(f => f.FolderName == folderName);

                if (existingFolderItem == null)
                {
                    // 如果不存在，则创建新的FolderItem并添加到集合中
                    var newFolderItem = new FolderItem(updatedFolderPath);
                    PopulateImages(newFolderItem);
                    Application.Current.Dispatcher.Invoke(() => Folders.Add(newFolderItem));
                }
                else
                {
                    // 如果存在，更新这个FolderItem的Images
                    existingFolderItem.Images.Clear();
                    PopulateImages(existingFolderItem);
                }
            }
        }

        private void PopulateImages(FolderItem folderItem)
        {
            string[] imagePaths = ExtractImagesFromFolder(folderItem.FolderPath);

            foreach (var imagePath in imagePaths)
            {
                var imageItem = new ImageItem(imagePath, folderItem);
                LoadImage(imageItem); // 可能需要调整此方法，以避免跨线程操作
                Application.Current.Dispatcher.Invoke(() => folderItem.Images.Add(imageItem));
            }
        }
    }
}
// 数据模型
public class FolderItem
{
    public string FolderName { get; set; }
    public ObservableCollection<ImageItem> Images { get; set; }
    public string FolderPath { get; private set; } // 添加这个属性

    public FolderItem(string path)
    {
        FolderName = Path.GetFileName(path);
        FolderPath = path; // 保存完整路径
        Images = new ObservableCollection<ImageItem>();
    }
}

public class ImageItem
{
    public string ImagePath { get; set; }
    public string ImageName { get; set; }
    public FolderItem ParentFolder { get; set; } // 添加此属性以引用父文件夹
    public string NewImagePath { get; set; }
    public BitmapImage ImageSource { get; set; }

    public ImageItem(string path, FolderItem parentFolder)
    {
        ImagePath = path;
        ImageName = System.IO.Path.GetFileName(path);
        ParentFolder = parentFolder; // 设置父文件夹引用
    }
}
