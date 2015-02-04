using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace WitSyncGUI.Converters
{
  /// <summary>
  /// Call this converter to change a True value to Visible and a False value to Collapsed
  /// </summary>
  /// <remarks>
  /// Source Paul Sheriff
  /// </remarks>
  public class BooleanToVisibilityConverter : IValueConverter
  {
    /// <summary>
    /// Convert a True/False value to Visibility.Visible/Visibility.Collapsed value
    /// </summary>
    /// <param name="value">A boolean value</param>
    /// <param name="targetType">The type of object</param>
    /// <param name="parameter">Any parameters passed via XAML</param>
    /// <param name="culture">The current culture</param>
    /// <returns>A Visibility Enumeration</returns>
    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
    {
      if ((bool)value)
        return Visibility.Visible;
      else
        return Visibility.Collapsed;
    }

    /// <summary>
    /// NOT IMPLEMENTED
    /// </summary>
    /// <param name="value">A boolean value</param>
    /// <param name="targetType">The type of object</param>
    /// <param name="parameter">Any parameters passed via XAML</param>
    /// <param name="culture">The current culture</param>
    /// <returns>NOT IMPLEMENTED</returns>
    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
    {
      throw new NotImplementedException("PDSABooleanToVisibility ConvertBack Method Not Implemented");
    }
  }
}