using Avalonia.Data.Converters;
using System.Globalization;

namespace AIDemon2.Converters
{
	public class StringEmptyConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return string.IsNullOrEmpty(value as string);
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}