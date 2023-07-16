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
        public Rectangle rect = new Rectangle
        {
            Stroke = Brushes.LightBlue,
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            RenderTransformOrigin = new Point(0.5, 0.5)
        };

        public TransformGroup transformGroup = new TransformGroup();

        Point startPoint, endPoint;

        RotateTransform rotateTransform = new RotateTransform();

        bool isDragging = false;
        public MainWindow()
        {
            InitializeComponent();


            // Initialize the RotateTransform and assign it to the rectangle
            rotateTransform = new RotateTransform();
            transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            rect.RenderTransform = transformGroup;

            // Add mouse events to the rectangle
            rect.MouseMove += Rect_MouseMove;
            rect.MouseLeftButtonDown += Rect_MouseLeftButtonDown;
            rect.MouseLeftButtonUp += Rect_MouseLeftButtonUp;

        }
        private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            rotateTransform.Angle = e.NewValue;
            UpdateTiledImage();
        }


        private void Rect_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                rect.Cursor = Cursors.SizeAll;
            }
            else if (isDragging)
            {
                Point position = e.GetPosition(ImageCanvas);
                double left = Canvas.GetLeft(rect) + position.X - startPoint.X;
                double top = Canvas.GetTop(rect) + position.Y - startPoint.Y;
                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);
                startPoint = position;
            }
        }

        // Start dragging the rectangle
        private void Rect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(ImageCanvas);
            rect.CaptureMouse();
            isDragging = true;

            // Stop the event from bubbling up to the canvas
            e.Handled = true;
        }

        // Stop dragging the rectangle
        private void Rect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            rect.ReleaseMouseCapture();
            isDragging = false;
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
            if (isDragging)
            {
                // If the rectangle is being dragged, don't start a new selection
                return;
            }

            // Check if the mouse is over the rectangle
            Point position = e.GetPosition(ImageCanvas);
            double left = Canvas.GetLeft(rect);
            double top = Canvas.GetTop(rect);
            if (position.X >= left && position.X <= left + rect.Width &&
                position.Y >= top && position.Y <= top + rect.Height)
            {
                // If the mouse is over the rectangle, don't start a new selection
                return;
            }

            // Remove the existing rectangle, if any
            ImageCanvas.Children.Remove(rect);

            // Start a new selection
            rect = new Rectangle { Stroke = Brushes.LightBlue, StrokeThickness = 2, Fill = Brushes.Transparent };
            rect.MouseMove += Rect_MouseMove;
            rect.MouseLeftButtonDown += Rect_MouseLeftButtonDown;
            rect.MouseLeftButtonUp += Rect_MouseLeftButtonUp;

            // Set RenderTransform of the new Rectangle to the TransformGroup
            rect.RenderTransform = transformGroup;

            startPoint = position;
            rect.Width = 0;
            rect.Height = 0;
            ImageCanvas.Children.Add(rect);
        }


        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Only resize the rectangle if a new selection is being created
            if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
            {
                endPoint = e.GetPosition(ImageCanvas);
                var width = Math.Abs(startPoint.X - endPoint.X);
                var height = Math.Abs(startPoint.Y - endPoint.Y);
                var left = Math.Min(startPoint.X, endPoint.X);
                var top = Math.Min(startPoint.Y, endPoint.Y);

                rect.Width = width;
                rect.Height = height;
                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);

                rect.Width = width;
                rect.Height = height;
                rect.RenderTransformOrigin = new Point(0.5, 0.5); // this line makes the rectangle rotate around its center

            }
        }

        private void ImageCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
          UpdateTiledImage();
        }

        private void UpdateTiledImage()
        {
            // Debug.Write("MouseUP ");
            if (rect != null && loadedImage != null)
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

                if (width > 5 && height > 5)
                {

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

                            // Create a new DrawingVisual to draw the rotated tile
                            // Create a new DrawingVisual to draw the rotated tile
                            DrawingVisual drawingVisual = new DrawingVisual();
                            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                            {
                                // Apply the transform to the drawing context
                                drawingContext.PushTransform(new TranslateTransform(width / 2, height / 2));
                                drawingContext.PushTransform(rotateTransform);
                                drawingContext.PushTransform(new TranslateTransform(-width / 2, -height / 2));

                                // Draw the selected image onto the DrawingVisual
                                drawingContext.DrawImage(selectedImage, new Rect(0, 0, width, height));

                                // Pop the transforms
                                drawingContext.Pop();
                                drawingContext.Pop();
                                drawingContext.Pop();
                            }
                            // Render the result to a bitmap
                            RenderTargetBitmap rotatedBitmap = new RenderTargetBitmap(width, height, loadedImage.DpiX, loadedImage.DpiY, PixelFormats.Default);
                            rotatedBitmap.Render(drawingVisual);

                            // Write the pixels from the rotated bitmap to the tiled image
                            int innerStride = rotatedBitmap.PixelWidth * (rotatedBitmap.Format.BitsPerPixel + 7) / 8;
                            byte[] innerPixelData = new byte[innerStride * rotatedBitmap.PixelHeight];
                            rotatedBitmap.CopyPixels(innerPixelData, innerStride, 0);
                            tiledImage.WritePixels(new Int32Rect(offsetX, offsetY, rotatedBitmap.PixelWidth, rotatedBitmap.PixelHeight), innerPixelData, innerStride, 0);

                        }
                    }

                    // Now tiledImage contains the tiled image.
                    TiledImage.Source = tiledImage;
                }
            }
        }

    }
}
