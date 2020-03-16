using System;
using System.Collections.Generic;
using System.Windows;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace RamanMapping.ViewModel
{
    public class MultiBoolAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
               object parameter, System.Globalization.CultureInfo culture)
        {
            foreach (var val in values)
                if (val is bool && (bool)val == false)
                    return false;

            return true;
        }
        public object[] ConvertBack(object value, Type[] targetTypes,
               object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class PointStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double x = ((Point)value).X;
            double y = ((Point)value).Y;
            return string.Format("{0:F1}", x) + ", " + string.Format("{0:F1}", y);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool v = Boolean.Parse(value.ToString());
            return v ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public static class Utils
    {
        public static void SaveUsingEncoder(string fileName, FrameworkElement UIElement, BitmapEncoder encoder)
        {
            int height = (int)UIElement.ActualHeight;
            int width = (int)UIElement.ActualWidth;
            UIElement.Measure(new System.Windows.Size(width, height));
            UIElement.Arrange(new Rect(0, 0, width, height));
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(UIElement);
            SaveUsingBitmapTargetRenderer(fileName, bitmap, encoder);
        }

        private static void SaveUsingBitmapTargetRenderer(string fileName, RenderTargetBitmap renderTargetBitmap, BitmapEncoder bitmapEncoder)
        {
            BitmapFrame frame = BitmapFrame.Create(renderTargetBitmap);
            bitmapEncoder.Frames.Add(frame);
            using(var stream = File.Create(fileName))
            {
                bitmapEncoder.Save(stream);
            }
        }
    }

    public static class DrawCanvas
    {
        public static Ellipse Circle(double x, double y, int width, int height, bool refPoint, Canvas cv)
        {

            Ellipse circle = new Ellipse()
            {
                Width = width,
                Height = height,
                Stroke = refPoint ? Brushes.Green : Brushes.Red,
                StrokeThickness = refPoint ? 10 : 5
            };

            cv.Children.Add(circle);

            circle.SetValue(Canvas.LeftProperty, x - width / 2.0);
            circle.SetValue(Canvas.TopProperty, y - height / 2.0);

            return circle;
        }

        public static TextBlock Text(double x, double y, string text, bool refPoint, SolidColorBrush color, Canvas cv, bool shift = true)
        {

            TextBlock textBlock = new TextBlock();

            textBlock.Text = text;

            textBlock.Foreground = color == null ? refPoint ? Brushes.Green : Brushes.Red : color;
            textBlock.FontSize = refPoint ? 36 : 24;
            textBlock.FontWeight = refPoint ? FontWeights.Bold : FontWeights.Normal;

            Canvas.SetLeft(textBlock, x - (shift ? (refPoint ? 10 : 7.5) : 0));
            Canvas.SetTop(textBlock, y - (shift ? (refPoint ? 58 : 40) : 10));

            cv.Children.Add(textBlock);

            return textBlock;
        }

        public static Rectangle Rect(double x, double y, int width, int height, SolidColorBrush color, Canvas cv, double opacity = 1.0)
        {
            Rectangle rect = new Rectangle()
            {
                Width = width,
                Height = height,
                Fill = color,
                Opacity = opacity,
                Stroke = color
            };

            cv.Children.Add(rect);
            Canvas.SetTop(rect, y);
            Canvas.SetLeft(rect, x);

            return rect;
        }
    }

}
