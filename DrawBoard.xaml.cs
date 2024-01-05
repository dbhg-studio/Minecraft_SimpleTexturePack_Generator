using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinecraftResourcePack_Builder
{
    /// <summary>
    /// DrawBoard.xaml 的交互逻辑
    /// </summary>
    public partial class DrawBoard : Window
    {
        private WriteableBitmap writeableBitmap;
        private string currentImagePath; // 用来存储当前图片路径

        public DrawBoard(string imagePath)
        {
            InitializeComponent();
            currentImagePath = imagePath; // 存储传入的图片路径
            LoadImage(imagePath); // 调用LoadImage方法加载图片
        }

        private void LoadImage(string imagePath)
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
                writeableBitmap = new WriteableBitmap(bitmapImage);
                DrawingCanvas.Background = new ImageBrush(writeableBitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (writeableBitmap == null)
                return;

            // 获取鼠标点击的位置
            Point point = e.GetPosition(DrawingCanvas);

            // 转换为整数
            int x = (int)point.X;
            int y = (int)point.Y;

            // 修改像素
            if (x < writeableBitmap.PixelWidth && y < writeableBitmap.PixelHeight)
            {
                // 设置像素颜色，这里设置为红色
                writeableBitmap.WritePixels(new Int32Rect(x, y, 1, 1), new[] { 0xFFFF0000 }, writeableBitmap.PixelWidth * 4, 0);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (FileStream fileStream = new FileStream(currentImagePath, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(fileStream);
                }
                MessageBox.Show("Image saved successfully.", "Save Image", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to save image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
