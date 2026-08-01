/// <summary>
/// Rozmowa z usługą AI nie doszła do skutku.
///
/// Istnieje po to, żeby <c>ChatService</c> przestał zamieniać każdy błąd na zwykłą
/// wiadomość i zapisywać ją do bazy jako pełnoprawną odpowiedź modelu. Taka „odpowiedź"
/// trafiała potem do eksportu i do historii, nie do odróżnienia od prawdziwej.
/// </summary>
public class ChatServiceException : Exception
{
	public ChatServiceException(string message) : base(message)
	{
	}

	public ChatServiceException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
