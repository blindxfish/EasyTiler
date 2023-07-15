using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EasyTiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage loadedImage;
        public Rectangle rect = new Rectangle {  Stroke = Brushes.LightBlue, StrokeThickness = 2 };
        Point startPoint, endPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp|All files (*.*)|*.*" // Filter files by extension
            };

            if (openFileDialog.ShowDialog() == true)
            {
                loadedImage = new BitmapImage(new Uri(openFileDialog.FileName));
                SourceImage.Source = loadedImage; // SourceImage is an Image control in your XAML
                SourceImage.Width = loadedImage.PixelWidth;
                SourceImage.Height = loadedImage.PixelHeight;
                ImageCanvas.Width = loadedImage.PixelWidth;
                ImageCanvas.Height = loadedImage.PixelHeight;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ImageCanvas.Children.Remove(rect);
        }

        private void ImageCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ImageCanvas.Children.Remove(rect);
            Debug.Write("MouseDown pressed ");
            startPoint = e.GetPosition(ImageCanvas);
            rect.Width = 0;
            rect.Height = 0;
            ImageCanvas.Children.Add(rect);
        }

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            //Debug.Write("MouseMove detected ");
            if (e.LeftButton == MouseButtonState.Released || rect == null)
                return;

            endPoint = e.GetPosition(ImageCanvas);
            var width = Math.Abs(startPoint.X - endPoint.X);
            var height = Math.Abs(startPoint.Y - endPoint.Y);
            var left = Math.Min(startPoint.X, endPoint.X);
            var top = Math.Min(startPoint.Y, endPoint.Y);

            rect.Width = width;
            rect.Height = height;
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
        }

        private void ImageCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
           // Debug.Write("MouseUP ");
            if (rect != null)
            {
                var scaleFactorX = loadedImage.PixelWidth / SourceImage.ActualWidth;
                var scaleFactorY = loadedImage.PixelHeight / SourceImage.ActualHeight;

                var x = (int)(Canvas.GetLeft(rect) * scaleFactorX);
                var y = (int)(Canvas.GetTop(rect) * scaleFactorY);
                var width = (int)(rect.Width * scaleFactorX);
                var height = (int)(rect.Height * scaleFactorY);

                var stride = width * ((loadedImage.Format.BitsPerPixel + 7) / 8);
                var size = height * stride;
                var pixelData = new byte[size];
                var rect2 = new Int32Rect(x, y, width, height);

                loadedImage.CopyPixels(rect2, pixelData, stride, 0);

                var selectedImage = new WriteableBitmap(width, height, loadedImage.DpiX, loadedImage.DpiY, loadedImage.Format, null);
                selectedImage.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);

                // Now selectedImage contains the selected part of the image.
                // Calculate how many tiles we need in each direction.
                var tilesX = loadedImage.PixelWidth / width;
                var tilesY = loadedImage.PixelHeight / height;

                var tiledImage = new WriteableBitmap(loadedImage.PixelWidth, loadedImage.PixelHeight, loadedImage.DpiX, loadedImage.DpiY, loadedImage.Format, null);

                for (var tileY = 0; tileY < tilesY; tileY++)
                {
                    for (var tileX = 0; tileX < tilesX; tileX++)
                    {
                        var offsetX = tileX * width;
                        var offsetY = tileY * height;
                        tiledImage.WritePixels(new Int32Rect(offsetX, offsetY, width, height), pixelData, stride, 0);
                    }
                }

                // Now tiledImage contains the tiled image.
                TiledImage.Source = tiledImage;
            }
        }

    }
}
