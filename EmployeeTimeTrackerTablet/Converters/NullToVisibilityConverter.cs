using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EmployeeTimeTrackerTablet.Converters
{
    /// <summary>
    /// Converter that removes question marks and other icon placeholders from the beginning of text.
    /// This allows the XAML to display proper icons separately while cleaning up the bound text.
    /// </summary>
    public class RemoveLeadingIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
            {
                // Remove leading question marks and common icon placeholders
                text = text.TrimStart('?', ' ');
                
                // Additional cleanup for multi-character sequences
                if (text.StartsWith("?? "))
                    text = text.Substring(3);
                else if (text.StartsWith("? "))
                    text = text.Substring(2);
                
                // Remove emoji and Unicode symbols (these need string-based removal)
                var iconsToRemove = new[] { "?", "?", "?", "?", "??", "??" };
                foreach (var icon in iconsToRemove)
                {
                    if (text.StartsWith(icon))
                    {
                        text = text.Substring(icon.Length).TrimStart();
                        break;
                    }
                }
                
                return text.Trim();
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed for one-way binding
            return value;
        }
    }

    /// <summary>
    /// Converter that handles null-to-visibility with parameter support for inversion.
    /// Enhanced version with better null handling.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverted = parameter?.ToString()?.ToLower() == "invert";
            bool isNull = value == null || (value is string str && string.IsNullOrEmpty(str));
            
            if (isInverted)
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return isNull ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}