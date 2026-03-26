using System.Globalization;
using System.Windows.Data;
using ScoreBoardVtk.Wpf.Localization;

namespace ScoreBoardVtk.Wpf.Converters;

public sealed class PortStatusTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var isConnected = values.Length > 0 && values[0] is bool typedIsConnected && typedIsConnected;
        var portName = values.Length > 1 ? values[1] as string : null;

        if (!isConnected)
        {
            return LocalizationManager.Instance.GetString("StatusPortClosed");
        }

        return string.IsNullOrWhiteSpace(portName)
            ? LocalizationManager.Instance.GetString("StatusPortOpen")
            : string.Format(
                culture,
                LocalizationManager.Instance.GetString("StatusPortOpenWithName"),
                portName);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
