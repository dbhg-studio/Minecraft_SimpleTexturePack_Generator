using AdonisUI.Controls;
using MinecraftResourcePack_Builder.lib;
using System;
using System.Collections.ObjectModel;
using System.IO;
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
    public partial class Template : AdonisWindow
    {
        public string ProjectName { get; private set; }
        public string ProjectPath { get; private set; }
        Tools tools = new Tools();
        public ObservableCollection<FolderItem> Folders { get; set; }

        public Template(string projectName = null, string projectPath = null)
        {
            ProjectName = projectName;
            ProjectPath = projectPath;

            InitializeComponent();

            Folders = new ObservableCollection<FolderItem>(); // 初始化 Folders
            FolderItemsControl.ItemsSource = Folders;
            DataContext = this;
            LoadFoldersAndImages();

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

            Progress<int> progress = new Progress<int>(value =>
            {
                ProgressBar.Value = value;
            });

            Task.Run(async () => await LoadData(progress)).ContinueWith(t =>
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
                string[] folderPaths = ExtractFolders(tools.TemplatePath(tools.Mcmeta(tools.ProjectPath(ProjectName, @"src\pack.mcmeta"))));
                if (folderPaths == null || folderPaths.Length == 0)
                {
                    return;
                }

                int total = folderPaths.Length;
                int currentProgress = 0;

                foreach (string folderPath in folderPaths)
                {
                    if (string.IsNullOrEmpty(folderPath))
                        continue;

                    FolderItem folderItem = new FolderItem(folderPath);
                    string[] imagePaths = ExtractImagesFromFolder(folderPath);

                    if (imagePaths == null)
                    {
                        continue;
                    }

                    foreach (string imagePath in imagePaths)
                    {
                        if (string.IsNullOrEmpty(imagePath))
                        {
                            continue;
                        }

                        ImageItem imageItem = new ImageItem(imagePath, folderItem);
                      await  LoadImageAsync(imageItem); // LoadImage now directly works with the file system
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

        private async Task LoadImageAsync(ImageItem imageItem)
        {
            try
            {
                BitmapImage bitmapImage = null;
                await Application.Current.Dispatcher.InvokeAsync(() => {
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
    }
}