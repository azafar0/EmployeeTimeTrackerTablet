using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace EmployeeTimeTrackerTablet.Converters
{
    /// <summary>
    /// Converter to handle string-to-visibility conversion for photo display
    /// Returns Visible if string is not null/empty, Collapsed otherwise
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                bool hasValue = !string.IsNullOrEmpty(stringValue);
                
                // Check if parameter is "invert" for inverse logic
                if (parameter?.ToString() == "invert")
                {
                    return hasValue ? Visibility.Collapsed : Visibility.Visible;
                }
                
                return hasValue ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// CRASH-SAFE Image Source Converter
    /// Safely converts file paths to BitmapImage objects with proper error handling
    /// </summary>
    public class SafeImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
            {
                try
                {
                    // Check if file exists first
                    if (System.IO.File.Exists(stringValue))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(stringValue, UriKind.Absolute);
                        bitmap.DecodePixelWidth = 50;
                        bitmap.DecodePixelHeight = 40;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Make it thread-safe
                        return bitmap;
                    }
                }
                catch
                {
                    // Return null on any error
                }
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to handle empty string to null conversion for Image Source binding
    /// Prevents WPF binding errors when trying to convert empty strings to ImageSource
    /// </summary>
    public class EmptyStringToNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ADDED: Inverse Boolean to Visibility Converter for photo display logic
    /// Returns Collapsed when true, Visible when false
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}