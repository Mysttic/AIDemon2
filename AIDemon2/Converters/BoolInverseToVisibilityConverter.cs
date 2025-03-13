using Avalonia.Data.Converters;
using System.Globalization;

namespace AIDemon2.Converters
{
	public class BoolInverseToVisibilityConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool b)
				return b ? "false" : "true";
			return "false";
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}