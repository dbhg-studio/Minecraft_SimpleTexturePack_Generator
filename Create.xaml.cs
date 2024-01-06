using AdonisUI.Controls;
using Minecraft_SimpleTexturePack_Generator.lib;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Minecraft_SimpleTexturePack_Generator
{
    /// <summary>
    /// Create.xaml 的交互逻辑
    /// </summary>
    public partial class Create : AdonisWindow
    {
        Tools Core = new Tools();
        public string ProjectName, ProjectVersion;
        string imagePath;

        public Create()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (PackName.Text != "" && PackVersion.SelectedItem != null)
            {
                if (!Core.Isfolder(Core.ProjectPath()))
                {
                    Directory.CreateDirectory(Core.ProjectPath());
                }
                ComboBoxItem selectedItem = (ComboBoxItem)PackVersion.SelectedItem;
                string Value = selectedItem.Tag.ToString();
                ProjectVersion = selectedItem.Content.ToString();
                CreateProject(PackName.Text, Value, PackIntroduction.Text, imagePath);
            }
            else
            {
                MessageBox.Show("名称和游戏版本不得为空", "错误");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PackIcon_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "图片文件 (*.png)|*.png";

            if (openFileDialog.ShowDialog() == true)
            {
                imagePath = openFileDialog.FileName;

                if (Core.IsImage(imagePath))
                {
                    BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
                    PackPng.Source = bitmapImage;
                    PackPng.IsEnabled = true;
                }
                else
                {
                    imagePath = null;
                    PackPng.IsEnabled = false;
                    MessageBox.Show("缩略图尺寸非1:1", "错误");
                }
            }
        }

        private void CreateProject(string name, string version, string Introduction = null, string packicon = null)
        {
            if (Core.Isfolder(Core.ProjectPath(name)) == true)
            {
                MessageBoxResult res = MessageBox.Show(Core.CustomizeMessage(name, "此项目已存在是否直接覆盖？", MessageBoxImage.Question));

                if (res == MessageBoxResult.OK)
                {
                    // 删除后再创建项目
                    if (Core.DeleteProjectFolder(name) == true)
                    {
                        if (Core.CreateProjectFolder(name))
                        {
                            if (packicon != null)
                            {
                                if (Core.CreatePack(packicon, name, true) == false)
                                {
                                    this.DialogResult = false;
                                }
                            }
                            if (Core.CreateMcmeta(name, version, Introduction))
                            {
                                ProjectName = name;
                                this.DialogResult = true;
                            }
                            else
                            {
                                this.DialogResult = false;
                            }
                        }
                        else
                        {
                            this.DialogResult = false;
                        }
                    }
                    else
                    {
                        this.DialogResult = false;
                    }
                }
                else
                {
                    this.DialogResult = false;
                }

            }
            else
            {
                // 创建项目
                if (Core.CreateProjectFolder(name))
                {
                    if (packicon != null)
                    {
                        if (Core.CreatePack(packicon, name) == false)
                        {
                            this.DialogResult = false;
                        }
                    }
                    if (Core.CreateMcmeta(name, version, Introduction))
                    {
                        ProjectName = name;
                        this.DialogResult = true;
                    }
                    else
                    {
                        this.DialogResult = false;
                    }
                }
                else
                {
                    this.DialogResult = false;
                }
            }
        }
    }
}
