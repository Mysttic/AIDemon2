using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIDemonV2.Converters;

public class AIMessageBackgroundConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		return value != null ? Brushes.DarkBlue : Brushes.DarkGray; // AI na niebiesko, użytkownik na szaro
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}