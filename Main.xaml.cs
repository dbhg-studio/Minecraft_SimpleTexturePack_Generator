using AdonisUI.Controls;
using MaterialDesignThemes.Wpf;
using MinecraftResourcePack_Builder.lib;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftResourcePack_Builder
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
            SoftwareVersion.Text = $"软件版本：{Assembly.GetEntryAssembly().GetName().Version.ToString()}";
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
                Template editor = new Template(selectedFile.Name);
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
                editor.Title = $@"{v} - 模板";
                Editor editor2 = new Editor(selectedFile.Name, 2)
                {
                    Title = $@"{selectedFile.Name} - 编辑器"
                };
                editor.Show();
                editor2.Show();
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            Create create = new Create();
            if (create.ShowDialog() == true)
            {
                this.Hide();
                Template editor = new Template(create.ProjectName)
                {
                    Title = $@"{create.ProjectVersion} - 模板"
                };
                Editor editor2 = new Editor(create.ProjectName, 2)
                {
                    Title = $@"{create.ProjectName} - 编辑器"
                };
                editor.Show();
                editor2.Show();
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