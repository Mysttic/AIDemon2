using Xunit;

namespace AIDemon2.Tests.Domain;

/// <summary>
/// Świeżo utworzona encja nie może uchodzić za zmodyfikowaną.
///
/// IsModified to ModificationDate > CreationDate, a widok rozmowy pokazuje pod tym
/// warunkiem drugi znacznik czasu. Obie daty brały się z osobnych odczytów
/// DateTime.UtcNow, który na Windows ma rozdzielczość 100 ns — różnica jednego tiku
/// wystarczała, by warunek był prawdziwy. W formacie HH:mm:ss różnica była
/// niewidoczna, więc użytkownik dostawał ten sam czas wypisany dwa razy.
///
/// Testy chodzą w pętli, bo przy dwóch odczytach zegara defekt trafiał mniej więcej
/// co drugą wiadomość — pojedyncza asercja przepuściłaby go w połowie przebiegów.
/// </summary>
public class MessageTimestampTests
{
	private const int Powtorzenia = 500;

	[Fact]
	public void Constructor_StampsBothDatesFromOneClockRead()
	{
		for (int i = 0; i < Powtorzenia; i++)
		{
			var wiadomosc = new Message("czesc");

			Assert.Equal(wiadomosc.CreationDate, wiadomosc.ModificationDate);
			Assert.False(wiadomosc.IsModified);
		}
	}

	[Fact]
	public void ObjectInitializer_StampsBothDatesFromOneClockRead()
	{
		// Kształt używany przez ChatService dla odpowiedzi modelu: konstruktor
		// bezparametrowy plus inicjalizator obiektu.
		for (int i = 0; i < Powtorzenia; i++)
		{
			var wiadomosc = new Message
			{
				MessageContent = "print(1)",
				OriginalMessage = "print(1)",
				IsUserMessage = false
			};

			Assert.Equal(wiadomosc.CreationDate, wiadomosc.ModificationDate);
			Assert.False(wiadomosc.IsModified);
		}
	}

	[Fact]
	public void IsModified_BecomesTrue_OnlyAfterRealEdit()
	{
		// Druga strona kontraktu: poprawka nie może wyłączyć wskaźnika zupełnie.
		var wiadomosc = new Message("czesc");

		wiadomosc.MessageContent = "czesc, swiecie";
		wiadomosc.ModificationDate = wiadomosc.CreationDate.AddMinutes(1);

		Assert.True(wiadomosc.IsModified);
	}
}
