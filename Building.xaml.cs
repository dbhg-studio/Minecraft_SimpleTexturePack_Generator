using AdonisUI.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Minecraft_SimpleTexturePack_Generator
{
    /// <summary>
    /// Building.xaml 的交互逻辑
    /// </summary>
    public partial class Building : AdonisWindow
    {
        private CancellationTokenSource cts;
        public string BuildPath;

        public Building(string startPath, string buildPath, string zipPath)
        {
            BuildPath = buildPath;
            InitializeComponent();
            _ = SomeMethodAsync(startPath, zipPath);

        }

        // 异步方法内部
        async Task SomeMethodAsync(string startPath, string zipPath)
        {
            Dispatcher.Invoke(() =>
            {
                Tips.Text = "准备打包中...";
                BuildProgress.IsIndeterminate = true;
            });
            await Task.Delay(1200); // 延迟3000毫秒（3秒钟）
            cts = new CancellationTokenSource();
            _ = Task.Run(() => ZipFolder(startPath, zipPath, cts.Token), cts.Token);
        }

        private void ZipFolder(string startPath, string zipPath, CancellationToken token)
        {
            try
            {
                // 更新进度条
                Dispatcher.Invoke(() =>
                {
                    BuildProgress.IsIndeterminate = false;
                    Tips.Text = "打包中...";
                });
                string[] files = Directory.GetFiles(startPath, "*", SearchOption.AllDirectories);
                int fileCount = files.Length;
                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        for (int i = 0; i < fileCount; i++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                // 如果取消请求被触发，关闭zip文件流，删除不完整的zip文件，然后退出
                                zipToOpen.Close();
                                File.Delete(zipPath);
                                return;
                            }

                            string file = files[i];
                            string entryName = file.Substring(startPath.Length + 1); // 相对路径
                            archive.CreateEntryFromFile(file, entryName);

                            // 更新进度条
                            Dispatcher.Invoke(() =>
                            {
                                BuildProgress.Value = (i + 1) * 100 / fileCount;
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 捕获取消操作的异常
                Dispatcher.Invoke(() =>
                {
                    Tips.Text = "用户取消打包";
                    BuildProgress.Value = 0;
                });
            }
            finally
            {
                // 完成后的操作
                Dispatcher.Invoke(() =>
                {
                    Tips.Text = "打包完成";
                    Open.Visibility = Visibility;
                    Open.IsEnabled = true;
                });
            }
        }

        private void Open_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("explorer.exe", BuildPath);
        }
    }
}
