using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDemonV2.Converters
{
	public class UtcToLocalTimeConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is DateTime utcDate)
			{
				return utcDate.ToLocalTime().ToString("HH:mm:ss"); // Konwersja do lokalnego czasu
			}
			return "";
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
