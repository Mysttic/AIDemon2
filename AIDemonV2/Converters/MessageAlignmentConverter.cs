using Avalonia.Data.Converters;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDemonV2.Converters;

public class MessageAlignmentConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		if (value is bool isUserMessage)
		{
			return isUserMessage ? HorizontalAlignment.Left : HorizontalAlignment.Right;
		}
		return HorizontalAlignment.Left;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
