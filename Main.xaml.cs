using AdonisUI.Controls;
using MaterialDesignThemes.Wpf;
using Minecraft_SimpleTexturePack_Generator.lib;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace Minecraft_SimpleTexturePack_Generator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class Main : AdonisWindow
    {
        Tools Core = new Tools();
        public string ProjectName;
        ObservableCollection<CompanyModel> TreeList;

        public Main()
        {
            InitializeComponent();
            SoftwareVersion.Text = $"软件版本：{Core.GetVersionGreater()}";
            TreeList = GetFileSystemObjects(Core.ProjectPath());
            FileList.ItemsSource = TreeList;
           

            this.Closing += delegate
            {
                Editor editor = new Editor();
                if (editor.IsLoaded == false)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    this.Hide();
                }
            };
        }

        private void FileList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = sender as TreeView;
            if (treeView?.SelectedItem is CompanyModel selectedFile && selectedFile.IsFolder) // 假设您的文件模型类名为YourFileModel，且有一个IsFolder属性来判断是否为文件夹
            {
                this.Hide();
                string v = "";
                switch (int.Parse(Core.Mcmeta(Core.ProjectPath(selectedFile.Name, @"src\pack.mcmeta"))))
                {
                    case 1:
                        v = "1.7.10";
                        break;
                    case 3:
                        v = "1.12.2";
                        break;
                    case 6:
                        v = "1.16.5";
                        break;
                };

                Open(selectedFile.Name,v);
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            Create create = new Create();
            if (create.ShowDialog() == true)
            {
                this.Hide();
                Open(create.ProjectName, create.ProjectVersion);
            }
        }

        public ObservableCollection<CompanyModel> GetFileSystemObjects(string path)
        {
            var items = new ObservableCollection<CompanyModel>();
            var dirInfo = new DirectoryInfo(path);

            // 添加文件夹
            foreach (var directory in dirInfo.GetDirectories())
            {
                var dirModel = new CompanyModel
                {
                    Name = directory.Name,
                    Path = directory.FullName,
                    IsFolder = true,
                    IconKind = PackIconKind.Folder
                };
                items.Add(dirModel);
            }

            return items;
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings();
            settings.ShowDialog();
        }

        private void Open(string ProjectName,string ProjectVersion,string ProjectPath = null)
        {
            if (Core.Isfolder(Core.TemplatePath()))
            {
                Directory.CreateDirectory(Core.TemplatePath());
            }
            if (!Core.Isfolder(Core.TemplatePath("1")) || !Core.Isfolder(Core.TemplatePath("6")) || !Core.Isfolder(Core.TemplatePath("6")))
            {
                TemplateDownload templateDownload = new TemplateDownload();
                if (templateDownload.ShowDialog() == true)
                {
                    Template template = new Template(ProjectName)
                    {
                        Title = $@"{ProjectVersion} - 模板"
                    };
                    Editor editor = new Editor(ProjectName, ProjectPath)
                    {
                        Title = $@"{ProjectName} - 编辑器"
                    };
                    template.Show();
                    editor.Show();
                }
            }
            else
            {
                Template template = new Template(ProjectName)
                {
                    Title = $@"{ProjectVersion} - 模板"
                };
                Editor editor = new Editor(ProjectName, ProjectPath)
                {
                    Title = $@"{ProjectName} - 编辑器"
                };
                template.Show();
                editor.Show();
            }

        }
    }
}
public class CompanyModel
{
    public string Name { get; set; }
    public string Path { get; set; }
    public ObservableCollection<CompanyModel> Children { get; set; }
    public bool IsFolder { get; set; }
    public PackIconKind IconKind { get; set; }

    public CompanyModel()
    {
        Children = new ObservableCollection<CompanyModel>();
    }
}