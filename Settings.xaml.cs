using AdonisUI.Controls;
using Minecraft_SimpleTexturePack_Generator.lib;

namespace Minecraft_SimpleTexturePack_Generator
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
                    Tools.IsChecked = true;
                    Mspaint.IsChecked = false;
                    break;
                default:
                    Tools.IsChecked = false;
                    Mspaint.IsChecked = true;
                    break;
            }
            DarwToolsPath.Text = Properties.Settings.Default.DarwTools;
        }

        private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void Ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((bool)Mspaint.IsChecked)
            {
                Properties.Settings.Default.ImageEditorTool = 0;
            }
            if ((bool)Tools.IsChecked)
            {
                Properties.Settings.Default.ImageEditorTool = 1;
            }

            Properties.Settings.Default.DarwTools = DarwToolsPath.Text;
            Properties.Settings.Default.Save();
            MessageBox.Show("保存成功", "设置");
            Close();
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
