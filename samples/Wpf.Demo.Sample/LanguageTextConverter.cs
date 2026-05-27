using System.Globalization;
using System.Windows.Data;

namespace Wpf.Demo.Sample;

public sealed class LanguageTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var lang = value as string ?? "ENG";
        var pair = parameter as string;
        if (string.IsNullOrWhiteSpace(pair))
            return string.Empty;

        var parts = pair.Split('|');
        if (parts.Length < 2)
            return pair;

        return string.Equals(lang, "KOR", StringComparison.OrdinalIgnoreCase) ? parts[1] : parts[0];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
