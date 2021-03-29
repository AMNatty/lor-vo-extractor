using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace LoRAudioExtractor.UI
{
    /// <summary>
    /// Interaction logic for AudioListView.xaml
    /// </summary>
    public partial class AudioListView
    {
        public AudioListView()
        {
            this.InitializeComponent();
        }
    }

    public class ByteSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] suffixes = { "B", "kB", "MB", "GB", "TB" };
            double len = (int) value;
            int order = 0;

            while (len >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:F2} {suffixes[order]}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
