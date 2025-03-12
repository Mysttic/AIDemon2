using Avalonia.Data.Converters;
using System.Globalization;

namespace AIDemonV2.Converters;

public class TextTruncateConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string text && int.TryParse(parameter?.ToString(), out int maxLength))
		{
			// zamiana znaków nowej linii na spacje
			text = text.Replace("\r\n", " ")
					   .Replace("\n", " ")
					   .Replace("\r", " ");

			// przycięcie
			return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
		}
		return value ?? string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> throw new NotImplementedException();
}