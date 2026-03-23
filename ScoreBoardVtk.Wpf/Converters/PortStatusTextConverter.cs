using System.Globalization;
using System.Windows.Data;

namespace ScoreBoardVtk.Wpf.Converters;

public sealed class PortStatusTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var isConnected = values.Length > 0 && values[0] is bool typedIsConnected && typedIsConnected;
        var portName = values.Length > 1 ? values[1] as string : null;

        if (!isConnected)
        {
            return "Port: closed";
        }

        return string.IsNullOrWhiteSpace(portName)
            ? "Port: open"
            : $"Port: open ({portName})";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
