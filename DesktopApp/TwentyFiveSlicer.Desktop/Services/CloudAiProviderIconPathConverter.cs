using System.Globalization;
using System.Windows.Data;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class CloudAiProviderIconPathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string providerId
            ? CloudAiProviderIconCatalog.GetIconPath(providerId)
            : CloudAiProviderIconCatalog.GetIconPath(string.Empty);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
