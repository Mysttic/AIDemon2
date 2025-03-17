using Avalonia.Data.Converters;
using System.Globalization;

namespace AIDemon2.Converters
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value is bool b && b ? "true" : "false";
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}