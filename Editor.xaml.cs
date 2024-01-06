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
        Photoshop photoshop = new Photoshop();
        Mspaint mspaint = new Mspaint();
        public ObservableCollection<EFolderItem> Folders { get; set; }
        public ObservableCollection<EImageItem> Images { get; set; }

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
                Folders = new ObservableCollection<EFolderItem>(); // 初始化 Folders
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
                switch (Properties.Settings.Default.ImageEditorTool)
                {
                    case 1:
                        if (!mspaint.Close())
                        {
                        }
                        break;
                    case 2:
                        if (Properties.Settings.Default.PhotoshopPath == null)
                        {
                            if (!mspaint.Close())
                            {
                            }
                            if (!aseprite.Close())
                            {
                            }
                        }
                        else
                        {
                            if (!photoshop.Close())
                            {
                            }
                        }
                        break;
                    default:
                        if (!aseprite.Close())
                        {
                        }
                        if (!mspaint.Close())
                        {
                        }
                        break;
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

                    var folderItem = new EFolderItem(folderPath);
                    var imagePaths = ExtractImagesFromFolder(folderPath);

                    if (imagePaths == null)
                        continue;

                    foreach (var imagePath in imagePaths)
                    {
                        if (string.IsNullOrEmpty(imagePath))
                            continue;

                        var imageItem = new EImageItem(imagePath, folderItem);
                        await LoadImageAsync(imageItem); // LoadImage now directly works with the file system
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

        private void LoadImage(EImageItem imageItem)
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

        private async Task LoadImageAsync(EImageItem imageItem)
        {
            try
            {
                BitmapImage bitmapImage = null;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 在UI线程上创建BitmapImage并进行初始化
                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.UriSource = new Uri(imageItem.ImagePath, UriKind.Absolute);
                    bitmapImage.DecodePixelWidth = 100; // 调整为合适的尺寸
                    bitmapImage.DecodePixelHeight = 100; // 调整为合适的尺寸
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // 允许跨线程访问
                });

                // 现在bitmapImage已经不可变，可以在任何线程上访问
                imageItem.ImageSource = bitmapImage;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
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
                    Filter = "*",
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
            // 判断是文件还是文件夹
            FileAttributes attr = File.GetAttributes(e.FullPath);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddNewFolderItem(e.FullPath);
                        }));
                        break;
                    case WatcherChangeTypes.Deleted:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RemoveFolderItem(e.FullPath);
                        }));
                        break;
                }
            }
            else
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // 确认图片项是否已经存在于UI集合
                            if (!ContainsImageItemWith(e.FullPath))
                            {
                                AddNewImageItem(e.FullPath);
                            }
                        }));
                        break;
                    case WatcherChangeTypes.Changed:
                        // 现有文件内容变更的逻辑保持不变
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Task.Delay(500).ContinueWith(t =>
                            {
                                RefreshImageItem(e.FullPath);
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }));
                        break;
                    case WatcherChangeTypes.Deleted:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RemoveImageItem(e.FullPath);
                        }));
                        break;
                }
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var folderPath = Path.GetDirectoryName(e.FullPath);
                var folderItem = Folders.FirstOrDefault(f => f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
                if (folderItem != null)
                {
                    var imageItem = folderItem.Images.FirstOrDefault(i => i.ImagePath == e.OldFullPath);
                    if (imageItem != null)
                    {
                        imageItem.ImagePath = e.FullPath;
                        imageItem.ImageName = Path.GetFileName(e.FullPath);
                        LoadImage(imageItem); // 重新加载图片以更新ImageSource
                    }
                }
            });
        }

        private bool ContainsImageItemWith(string imagePath)
        {
            foreach (var folder in Folders)
            {
                if (folder.Images.Any(img => img.ImagePath.Equals(imagePath, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        private void AddNewFolderItem(string folderPath)
        {
            var newFolderItem = new EFolderItem(folderPath);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Folders.Add(newFolderItem);
               // FolderItemsControl.Items.Refresh(); // 强制刷新FolderItemsControl以显示新项
               PopulateImages(newFolderItem); // 加载新文件夹中的图片
            });
        }

        private void RemoveFolderItem(string folderPath)
        {
            var folderItemToRemove = Folders.FirstOrDefault(f => f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (folderItemToRemove != null)
                {
                    Folders.Remove(folderItemToRemove);
                    // 可能还需要刷新UI以确保移除的项目不再显示
                }
            });
        }

        private void AddNewImageItem(string imagePath)
        {
            // 可能需要在这里提取父文件夹路径
            string folderPath = Path.GetDirectoryName(imagePath);
            // 查找对应的EFolderItem
            var folderItem = Folders.FirstOrDefault(f => f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
            var imageItem = new EImageItem(imagePath, folderItem);
            Application.Current.Dispatcher.Invoke(async () =>
            {
                // 在添加之前检查图片是否已经存在于集合中
                if (!folderItem.Images.Any(img => img.ImagePath.Equals(imagePath, StringComparison.OrdinalIgnoreCase)))
                {
                    folderItem.Images.Add(imageItem);
                   await LoadImageAsync(imageItem); // 异步加载图片
                }
            });
        }

        private void RemoveImageItem(string imagePath)
        {
            // 查找包含该图片的文件夹项
            foreach (var folderItem in Folders)
            {
                var imageItemToRemove = folderItem.Images.FirstOrDefault(i => i.ImagePath.Equals(imagePath, StringComparison.OrdinalIgnoreCase));
                if (imageItemToRemove != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 从该文件夹的图片集合中移除图片项
                        folderItem.Images.Remove(imageItemToRemove);
                        // 如果需要，这里也可以通知UI刷新
                        folderItem.OnPropertyChanged(nameof(EFolderItem.Images));
                    });
                    break;
                }
            }
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
                        Task.Delay(500).ContinueWith(async (t) =>
                        {
                           await LoadImageAsync(imageItem);
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    });

                    break;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 这里需要先尝试释放旧的BitmapImage资源，以避免文件占用的问题
                        if (imageItem.ImageSource != null)
                        {
                            imageItem.ImageSource = null;
                        }

                        // 稍微延迟加载新图片，以确保文件系统已经完成了写操作
                        Task.Delay(500).ContinueWith(async (t) =>
                        {
                            await LoadImageAsync(imageItem);
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
                var updatedFolders = new ObservableCollection<EFolderItem>();

                foreach (var folderPath in folderPaths)
                {
                    var folderItem = new EFolderItem(folderPath);
                    var imagePaths = ExtractImagesFromFolder(folderPath);

                    foreach (var imagePath in imagePaths)
                    {
                        var imageItem = new EImageItem(imagePath, folderItem);
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

        private async void PopulateImages(EFolderItem folderItem)
        {
            string[] imagePaths = ExtractImagesFromFolder(folderItem.FolderPath);
            foreach (var imagePath in imagePaths)
            {
                var imageItem = new EImageItem(imagePath, folderItem);
                folderItem.Images.Add(imageItem); // 这里先添加图片项
                await imageItem.RefreshAsync(); // 然后异步地刷新图片
            }
            folderItem.OnPropertyChanged(nameof(EFolderItem.Images));
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private BitmapImage LoadBitmapFromPath(string path)
        {
            BitmapImage bitmap = new BitmapImage();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 这非常重要
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze(); // 确保图片可以跨线程使用
            return bitmap;
        }

    }
}
// 数据模型
public class EFolderItem : INotifyPropertyChanged
{
    public string FolderName { get; set; }
    public ObservableCollection<EImageItem> Images { get; set; }
    public string FolderPath { get; private set; } // 添加这个属性

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public EFolderItem(string path)
    {
        FolderName = Path.GetFileName(path);
        FolderPath = path; // 保存完整路径
        Images = new ObservableCollection<EImageItem>();
    }
}

public class EImageItem : INotifyPropertyChanged
{
    public string ImagePath { get; set; }
    public string ImageName { get; set; }
    public EFolderItem ParentFolder { get; set; } // 添加此属性以引用父文件夹
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

    public async Task RefreshAsync()
    {
        // 先释放原先的BitmapImage资源
        if (ImageSource != null)
        {
            ImageSource = null;
            OnPropertyChanged(nameof(ImageSource));
        }

        // 重新加载图片
        await LoadImageAsync(this).ConfigureAwait(false);
    }

    public async Task LoadImageAsync(EImageItem imageItem)
    {
        try
        {
            BitmapImage bitmapImage = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 在UI线程上创建BitmapImage并进行初始化
                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = new Uri(imageItem.ImagePath, UriKind.Absolute);
                bitmapImage.DecodePixelWidth = 100; // 调整为合适的尺寸
                bitmapImage.DecodePixelHeight = 100; // 调整为合适的尺寸
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // 允许跨线程访问
            });

            // 现在bitmapImage已经不可变，可以在任何线程上访问
            imageItem.ImageSource = bitmapImage;
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public EImageItem(string path, EFolderItem parentFolder)
    {
        ImagePath = path;
        ImageName = System.IO.Path.GetFileName(path);
        ParentFolder = parentFolder; // 设置父文件夹引用
    }
}
