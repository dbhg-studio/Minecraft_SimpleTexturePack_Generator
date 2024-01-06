using AdonisUI.Controls;
using MinecraftResourcePack_Builder.lib;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftResourcePack_Builder
{
    /// <summary>
    /// TemplateDownload.xaml 的交互逻辑
    /// </summary>
    public partial class TemplateDownload : AdonisWindow
    {
        Tools tools = new Tools();
        CancellationTokenSource cts = new CancellationTokenSource();

        public int an = 0;

        public TemplateDownload()
        {
            InitializeComponent();
            Start();
            Closing += delegate
            {
                cts.Cancel();
            };
        }

        public async Task Start()
        {
            CancellationToken token = cts.Token;
            Tips.Text = "检查模板完整性中...";
            DProgress.IsIndeterminate = true;
            if (tools.Isfolder(tools.TemplatePath()))
            {
                if (tools.Isfolder(tools.TemplatePath("1")))
                {
                    an += 1;
                    Tips.Text = $"检查模板完整性中... {an}/4";
                    await Task.Delay(2000);
                }
                if (tools.Isfolder(tools.TemplatePath("3")))
                {
                    an += 1;
                    Tips.Text = $"检查模板完整性中... {an}/4";
                    await Task.Delay(2000);
                }
                if (tools.Isfolder(tools.TemplatePath("6")))
                {
                    an += 1;
                    Tips.Text = $"检查模板完整性中... {an}/4";
                    await Task.Delay(2000);
                }
                an += 1;
                Tips.Text = $"检查模板完整性中... {an}/4";
                await Task.Delay(2000);
            }
            else
            {
                Directory.CreateDirectory(tools.TemplatePath());
                await Task.Delay(2000);
            }
            if (an == 4)
            {
                Tips.Text = "模板完整，无需下载";
                DProgress.IsIndeterminate = false;
                DProgress.Value = 100;
                Ok.Visibility = Visibility;
                return;
            }
            else
            {
                Tips.Text = "模板不完整，获取下载链接中...";
                DProgress.IsIndeterminate = true;
                await Task.Delay(2000);
                string url = "https://webstatic.dbhg.top/MinecraftResourcePack_Builder/Template.zip"; // 替换为你的ZIP文件URL
                string zipPath = $@"{tools.TemplatePath()}\Template.zip"; // ZIP文件保存路径
                string extractPath = tools.TemplatePath(); // 解压ZIP文件到这个文件夹
                try
                {
                    await DownloadFileAsync(url, zipPath, token);
                    await ExtractFileAsync(zipPath, extractPath, token);
                    await DelCache(zipPath, token);
                }
                catch (Exception ex)
                {
                    Tips.Text = "错误失败" + ex.Message;
                }
            }
        }

        private async Task DownloadFileAsync(string url, string outputPath, CancellationToken cancellationToken)
        {
            Tips.Text = "下载中...";
            DProgress.IsIndeterminate = false;
            DProgress.Value = 0;
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        long totalRead = 0L;
                        byte[] buffer = new byte[8192];
                        bool isMoreToRead = true;

                        do
                        {
                            // 检查是否请求了取消
                            cancellationToken.ThrowIfCancellationRequested();
                            int read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;
                                UpdateProgress(DProgress, totalRead, totalBytes);
                            }
                        }
                        while (isMoreToRead);
                    }
                }
            }
        }
        private async Task ExtractFileAsync(string zipPath, string extractPath, CancellationToken cancellationToken)
        {
            DProgress.IsIndeterminate = false;
            await Task.Run(() =>
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    int totalEntries = archive.Entries.Count;
                    int entryIndex = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // 检查是否请求了取消
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!string.IsNullOrEmpty(entry.Name)) // Ignore directories
                        {
                            string destinationPath = Path.Combine(extractPath, entry.FullName);
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            entry.ExtractToFile(destinationPath, true);

                            entryIndex++;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (entryIndex % 10 == 0) // 更新UI的频率
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        Tips.Text = $"解压中... - {entryIndex}/{totalEntries}";
                                    });
                                }
                            });
                        }
                    }
                }
            });
        }
        private async Task DelCache(string zipPath, CancellationToken cancellationToken)
        {
            Tips.Text = "准备删除缓存中...";
            await Task.Delay(2000); // 延迟3000毫秒（3秒钟）
            try
            {
                // 检查文件是否存在
                if (File.Exists(zipPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.SetAttributes(zipPath, FileAttributes.Normal);
                    File.Delete(zipPath);
                    Tips.Text = "删除缓存成功";
                    Ok.Visibility = Visibility;
                }
                else
                {
                    Tips.Text = "没有缓存文件";
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                Tips.Text = $"没有权限：{ uae.Message}";
            }
            catch (IOException e)
            {
                Tips.Text = $"文件占用或其他IO错误：{e.Message}";
            }
            catch (Exception e)
            {
                Tips.Text = $"删除缓存失败：{e.Message}";
            }
        }

        private void UpdateProgress(ProgressBar progressBar, long value, long total)
        {
            if (total <= 0)
            {
                return; // To avoid division by zero
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                progressBar.Value = value * 100 / total;
            });
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
