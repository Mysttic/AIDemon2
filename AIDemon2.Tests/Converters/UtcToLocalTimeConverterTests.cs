using AIDemon2.Converters;
using System.Globalization;
using Xunit;

namespace AIDemon2.Tests.Converters;

public class UtcToLocalTimeConverterTests
{
	private readonly UtcToLocalTimeConverter _converter = new();

	private string? Convert(object? value) =>
		(string?)_converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

	[Fact]
	public void Convert_TreatsUnspecifiedAsUtc()
	{
		// EF Core czyta z SQLite daty jako DateTimeKind.Unspecified. DateTime.ToLocalTime()
		// traktuje taką wartość jak CZAS LOKALNY i zwraca ją bez zmiany — więc znaczniki
		// czasu wiadomości wyświetlały się przesunięte o offset strefy.
		var zapisaneWBazie = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

		string oczekiwane = TimeZoneInfo
			.ConvertTimeFromUtc(DateTime.SpecifyKind(zapisaneWBazie, DateTimeKind.Utc), TimeZoneInfo.Local)
			.ToString("HH:mm:ss");

		Assert.Equal(oczekiwane, Convert(zapisaneWBazie));
	}

	[Fact]
	public void Convert_ConvertsUtcToLocal()
	{
		var utc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

		Assert.Equal(utc.ToLocalTime().ToString("HH:mm:ss"), Convert(utc));
	}

	[Fact]
	public void Convert_ReturnsEmpty_ForNull()
	{
		Assert.Equal(string.Empty, Convert(null));
	}

	[Fact]
	public void Convert_ReturnsEmpty_ForNonDateValue()
	{
		Assert.Equal(string.Empty, Convert("nie data"));
	}
}
