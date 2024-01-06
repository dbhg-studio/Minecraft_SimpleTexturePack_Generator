using AdonisUI.Controls;
using MinecraftResourcePack_Builder.lib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
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
        CancellationTokenSource cancellationTokenSource;
        private FileSystemWatcher _watcher;
        public string ProjectName { get; private set; }
        public string ProjectPath { get; private set; }
        public int OpenType { get; private set; }
        Tools tools = new Tools();
        Aseprite aseprite = new Aseprite();
        public ObservableCollection<FolderItem> Folders { get; set; }
        public ObservableCollection<ImageItem> Images { get; set; }

        public Editor(string projectName = null, int openType = 0, string projectPath = null)
        {
            if (projectName == null && openType == 0 && projectPath == null)
            {
                InitializeComponent();
                return;
            }
            else
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
            }

            this.Closing += delegate
            {
                if (!aseprite.Close())
                {
                    MessageBox.Show("关闭失败");
                }
                Application.Current.Shutdown();
            };
        }

        private void LoadFoldersAndImages()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Assume we show a progress bar
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 0;

            var progress = new Progress<int>(value =>
            {
                ProgressBar.Value = value;
            });

            Task.Run(async () =>
            {
                // 把token传递给需要进行长时间操作的方法
                await LoadData(progress);
            }, token).ContinueWith(t =>
            {
                // Hide progress bar when done
                ProgressBar.Visibility = Visibility.Hidden;
                LoadingOverlay.Visibility = Visibility.Hidden;
                EditorMain.Children.Remove(LoadingOverlay);
                if (t.IsCanceled)
                {
                    // 任务被取消时的处理
                }
                else if (t.Exception != null)
                {
                    // 出现异常时的处理
                    MessageBox.Show(t.Exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
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
            string imagePath = imageItem.ImagePath;

            try
            {
                if (File.Exists(imagePath))
                {
                    BitmapImage bitmapImage = new BitmapImage();

                    // 使用一个唯一的URI加载图像以绕过缓存
                    using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        // 使用流而不是直接使用URI来避免缓存问题
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze(); // 为了确保图像可以跨线程使用
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        imageItem.ImageSource = bitmapImage;
                        imageItem.OnPropertyChanged(nameof(ImageItem.ImageSource));
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Failed to load image: " + ex.Message));
            }
        }

        private bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // 文件被占用或无法访问
                return true;
            }

            // 文件可以访问
            return false;
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is ImageItem imageItem)
            {
                if (!aseprite.Open(imageItem.ImagePath))
                {
                    MessageBox.Show("打开失败");
                }
            }
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            tools.BuildProject(ProjectName);
        }

        private void InitializeFileSystemWatcher()
        {
            try
            {
                // 设置 FileSystemWatcher来监视图片文件的变化
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
                MessageBox.Show($"设置监视器时出现错误 - {ex.Message}");
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // 使用BeginInvoke在UI线程上异步执行，添加一个延迟以等待文件写入完成
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 稍微延迟再尝试加载图片
                Task.Delay(500).ContinueWith(t =>
                {
                    RefreshImageItem(e.FullPath);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }));
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // 使用InvokeAsync以避免在UI线程上造成阻塞
            Application.Current.Dispatcher.InvokeAsync(() => UpdateUI());
        }

        private void RefreshImageItem(string imagePath)
        {
            foreach (var folder in Folders)
            {
                var imageItem = folder.Images.FirstOrDefault(i => i.ImagePath.Equals(imagePath, StringComparison.OrdinalIgnoreCase));
                if (imageItem != null)
                {
                    // 重新加载图像项
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 这里需要先尝试释放旧的BitmapImage资源，以避免文件占用的问题
                        if (imageItem.ImageSource != null)
                        {
                            imageItem.ImageSource = null;
                        }

                        // 稍微延迟加载新图片，以确保文件系统已经完成了写操作
                        Task.Delay(500).ContinueWith((t) =>
                        {
                            LoadImage(imageItem);
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    });

                    break;
                }
            }
        }

        // 你可能需要编写这个函数来更新UI元素
        private void UpdateUI()
        {
            // 这里调用你的UI更新逻辑
            try
            {
                // 检索更新后的文件夹和图片信息
                var folderPaths = ExtractFolders(tools.ProjectPath(ProjectName, @"src\assets\minecraft\textures"));
                var updatedFolders = new ObservableCollection<FolderItem>();

                foreach (var folderPath in folderPaths)
                {
                    var folderItem = new FolderItem(folderPath);
                    var imagePaths = ExtractImagesFromFolder(folderPath);

                    foreach (var imagePath in imagePaths)
                    {
                        var imageItem = new ImageItem(imagePath, folderItem);
                        LoadImage(imageItem);
                        folderItem.Images.Add(imageItem);
                    }

                    updatedFolders.Add(folderItem);
                }

                // 将Folders更新为最新的集合
                Folders = updatedFolders;

                // 确保FolderItemsControl绑定到新的Folders集合
                FolderItemsControl.ItemsSource = Folders;

                // 刷新FolderItemsControl
                FolderItemsControl.Items.Refresh();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新UI时出现问题 - {ex.Message}");
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            foreach (var folder in Folders)
            {
                foreach (var image in folder.Images)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LoadImage(image);
                    });
                }
            }

            // 通知ItemsControl刷新
            FolderItemsControl.Items.Refresh();
        }


    }
}
// 数据模型
public class FolderItem: INotifyPropertyChanged
{
    public string FolderName { get; set; }
    public ObservableCollection<ImageItem> Images { get; set; }
    public string FolderPath { get; private set; } // 添加这个属性

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public FolderItem(string path)
    {
        FolderName = Path.GetFileName(path);
        FolderPath = path; // 保存完整路径
        Images = new ObservableCollection<ImageItem>();
    }
}

public class ImageItem : INotifyPropertyChanged
{
    public string ImagePath { get; set; }
    public string ImageName { get; set; }
    public FolderItem ParentFolder { get; set; } // 添加此属性以引用父文件夹
    public string NewImagePath { get; set; }
    private BitmapImage _imageSource;
    public BitmapImage ImageSource
    {
        get => _imageSource;
        set
        {
            if (_imageSource != value)
            {
                _imageSource = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ImageItem(string path, FolderItem parentFolder)
    {
        ImagePath = path;
        ImageName = System.IO.Path.GetFileName(path);
        ParentFolder = parentFolder; // 设置父文件夹引用
    }
}
