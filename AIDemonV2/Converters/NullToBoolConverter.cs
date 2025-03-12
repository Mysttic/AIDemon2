﻿using Avalonia.Data.Converters;
using System.Globalization;

namespace AIDemonV2.Converters
{
	public class NullToBoolConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value != null;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}