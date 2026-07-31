using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

public class Message : EntityBase, IMessage
{
	private string _messageContent = string.Empty;
	public string MessageContent
	{
		get => _messageContent;
		set => SetProperty(ref _messageContent, value);
	}

	private string _originalMessage = string.Empty;
	public string OriginalMessage
	{
		get => _originalMessage;
		set => SetProperty(ref _originalMessage, value);
	}

	private string? _aiModel;
	public string? AIModel
	{
		get => _aiModel;
		set => SetProperty(ref _aiModel, value);
	}

	private string? _programmingLanguage;
	public string? ProgrammingLanguage
	{
		get => _programmingLanguage;
		set => SetProperty(ref _programmingLanguage, value);
	}

	private bool _favourite;
	public bool Favourite
	{
		get => _favourite;
		set => SetProperty(ref _favourite, value);
	}

	private bool _isUserMessage;
	/// <summary>
	/// Autor wiadomości. Wcześniej wyliczane jako <c>string.IsNullOrEmpty(ProgrammingLanguage)</c>,
	/// czyli "wiadomość bez języka programowania musi być od użytkownika".
	///
	/// Nie musi: język bierze się z ustawień, a te są puste, dopóki użytkownik go
	/// nie wybierze. Na świeżej instalacji WSZYSTKIE odpowiedzi modelu wyświetlały
	/// się więc jak własne wiadomości użytkownika — wyrównane do prawej i bez
	/// przycisków akcji. Autor to fakt o wiadomości, nie funkcja jej treści.
	/// </summary>
	public bool IsUserMessage
	{
		get => _isUserMessage;
		set => SetProperty(ref _isUserMessage, value);
	}

	// Poniższe nie są bindowane w widokach — zwykłe właściwości wystarczą.
	public bool Deleted { get; set; }

	public int? ReplyToMessageId { get; set; }

	[ForeignKey("ReplyToMessageId")]
	public Message? ReplyTo { get; set; }

	public ICollection<Message> Replies { get; set; } = new List<Message>();

	public bool IsModified => ModificationDate > CreationDate;

	/// <summary>
	/// Właściwości wyliczane muszą zgłaszać zmianę razem ze swoimi źródłami.
	/// Wcześniej robił to weaver Fody; teraz robi to jawny kod, widoczny w C#.
	/// </summary>
	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.PropertyName is nameof(CreationDate) or nameof(ModificationDate))
			base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsModified)));
	}

	public Message()
	{
	}

	/// <param name="isUserMessage">
	/// Domyślnie true: ten konstruktor obsługuje wiadomości wpisywane w oknie
	/// rozmowy. Odpowiedzi modelu i komunikaty systemowe muszą podać false.
	/// </param>
	public Message(string messageContent, bool isUserMessage = true)
	{
		MessageContent = messageContent;
		OriginalMessage = messageContent;
		CreationDate = DateTime.UtcNow;
		ModificationDate = DateTime.UtcNow;
		AIModel = null;
		IsUserMessage = isUserMessage;
	}
}
