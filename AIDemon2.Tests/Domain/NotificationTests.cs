using System.ComponentModel;
using Xunit;

namespace AIDemon2.Tests.Domain;

/// <summary>
/// Powiadomienia o zmianie właściwości były dotąd wplatane w assembly przez weaver
/// PropertyChanged.Fody — nie było ich widać w kodzie C# i ich utrata nie dawała
/// żadnego błędu kompilacji, tylko cicho martwy interfejs. Te testy pilnują każdej
/// właściwości, która jest realnie bindowana w widokach.
/// </summary>
public class NotificationTests
{
	private static List<string> Nasluchuj(INotifyPropertyChanged obiekt, Action akcja)
	{
		var zmiany = new List<string>();
		PropertyChangedEventHandler handler = (_, e) => zmiany.Add(e.PropertyName!);
		obiekt.PropertyChanged += handler;
		try
		{
			akcja();
		}
		finally
		{
			obiekt.PropertyChanged -= handler;
		}
		return zmiany;
	}

	[Fact]
	public void Message_ImplementsNotification()
	{
		Assert.IsAssignableFrom<INotifyPropertyChanged>(new Message());
	}

	[Theory]
	[InlineData(nameof(Message.MessageContent))]
	[InlineData(nameof(Message.OriginalMessage))]
	[InlineData(nameof(Message.AIModel))]
	[InlineData(nameof(Message.ProgrammingLanguage))]
	public void Message_RaisesForBoundStringProperties(string wlasciwosc)
	{
		var wiadomosc = new Message();
		var setter = typeof(Message).GetProperty(wlasciwosc)!;

		var zmiany = Nasluchuj(wiadomosc, () => setter.SetValue(wiadomosc, "nowa wartosc"));

		Assert.Contains(wlasciwosc, zmiany);
	}

	[Fact]
	public void Message_RaisesForFavourite()
	{
		var wiadomosc = new Message();

		var zmiany = Nasluchuj(wiadomosc, () => wiadomosc.Favourite = true);

		Assert.Contains(nameof(Message.Favourite), zmiany);
	}

	[Fact]
	public void Message_DoesNotRaise_WhenValueUnchanged()
	{
		var wiadomosc = new Message { MessageContent = "ta sama" };

		var zmiany = Nasluchuj(wiadomosc, () => wiadomosc.MessageContent = "ta sama");

		Assert.Empty(zmiany);
	}

	[Fact]
	public void Message_RaisesIsModified_WhenModificationDateChanges()
	{
		// IsModified jest bindowane w MainChatView; jako właściwość wyliczana
		// nie zgłosi się samo — musi je podnieść setter źródła.
		var wiadomosc = new Message("tresc");

		var zmiany = Nasluchuj(wiadomosc,
			() => wiadomosc.ModificationDate = wiadomosc.CreationDate.AddMinutes(1));

		Assert.Contains(nameof(Message.IsModified), zmiany);
		Assert.True(wiadomosc.IsModified);
	}

	[Fact]
	public void Message_RaisesIsUserMessage_WhenAuthorChanges()
	{
		var wiadomosc = new Message("tresc");

		var zmiany = Nasluchuj(wiadomosc, () => wiadomosc.IsUserMessage = false);

		Assert.Contains(nameof(Message.IsUserMessage), zmiany);
		Assert.False(wiadomosc.IsUserMessage);
	}

	[Fact]
	public void Message_KeepsAuthor_WhenProgrammingLanguageChanges()
	{
		// Autor wiadomości był wyliczany z języka programowania: ustawienie języka
		// zmieniało nadawcę. Teraz to niezależne fakty.
		var wiadomosc = new Message("tresc");

		wiadomosc.ProgrammingLanguage = "python";

		Assert.True(wiadomosc.IsUserMessage);
	}

	[Theory]
	[InlineData(nameof(EntityBase.CreationDate))]
	[InlineData(nameof(EntityBase.ModificationDate))]
	public void EntityBase_RaisesForDates(string wlasciwosc)
	{
		var wiadomosc = new Message();
		var setter = typeof(Message).GetProperty(wlasciwosc)!;

		var zmiany = Nasluchuj(wiadomosc,
			() => setter.SetValue(wiadomosc, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

		Assert.Contains(wlasciwosc, zmiany);
	}

	[Fact]
	public void Message_RaisesOncePerChange()
	{
		// Przy trzech nakładających się mechanizmach MVVM (Fody obok ObservableObject)
		// każda zmiana emitowała PropertyChanged 2-3 razy, co powodowało wielokrotne
		// przeliczanie bindingów i CanExecute.
		var wiadomosc = new Message();

		var zmiany = Nasluchuj(wiadomosc, () => wiadomosc.MessageContent = "x");

		Assert.Equal(1, zmiany.Count(z => z == nameof(Message.MessageContent)));
	}
}
