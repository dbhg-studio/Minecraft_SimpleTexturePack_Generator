using AduSkin.Controls.Metro;
using System.Windows;

namespace MinecraftResourcePack_Builder
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //初始化通知弹框
            NoticeManager.Initialize();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            NoticeManager.ExitNotification();
            base.OnExit(e);
        }
    }
}
