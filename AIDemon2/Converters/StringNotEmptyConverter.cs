using Avalonia.Data.Converters;
using System.Globalization;

namespace AIDemon2.Converters;

public class StringNotEmptyConverter : IValueConverter
{
	public static readonly StringNotEmptyConverter Instance = new();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return !string.IsNullOrWhiteSpace(value as string);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}