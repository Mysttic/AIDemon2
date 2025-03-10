using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AIDemonV2.Converters;

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
