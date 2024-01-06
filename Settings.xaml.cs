using AdonisUI.Controls;
using MinecraftResourcePack_Builder.lib;

namespace MinecraftResourcePack_Builder
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : AdonisWindow
    {
        Tools tools = new Tools();
        public Settings()
        {
            InitializeComponent();
            switch (Properties.Settings.Default.ImageEditorTool)
            {
                case 1:
                    Aseprite.IsChecked = false;
                    Mspaint.IsChecked = true;
                    Photoshop.IsChecked = false;
                    break;
                case 2:
                    Aseprite.IsChecked = false;
                    Mspaint.IsChecked = false;
                    Photoshop.IsChecked = true;
                    break;
                default:
                    Aseprite.IsChecked = true;
                    Mspaint.IsChecked = false;
                    Photoshop.IsChecked = false;
                    break;
            }
            PhotoshopPath.Text = Properties.Settings.Default.PhotoshopPath;
        }

        private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void Ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((bool)Aseprite.IsChecked)
            {
                Properties.Settings.Default.ImageEditorTool = 0;
            }
            if ((bool)Mspaint.IsChecked)
            {
                Properties.Settings.Default.ImageEditorTool = 1;
            }
            if ((bool)Photoshop.IsChecked)
            {
                Properties.Settings.Default.ImageEditorTool = 2;
            }
            Properties.Settings.Default.PhotoshopPath = PhotoshopPath.Text;
            Properties.Settings.Default.Save();
            MessageBox.Show("保存成功", "设置");
        }

        private void TemplateDowload_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TemplateDownload templateDownload = new TemplateDownload();
            templateDownload.ShowDialog();
        }

        private void Gitee_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            tools.OpenLink("https://gitee.com/dbhg/Minecraft_SimpleTexturePack_Generator");
        }

        private void Github_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            tools.OpenLink("https://github.com/dbhg-studio/Minecraft_SimpleTexturePack_Generator");
        }
    }
}
