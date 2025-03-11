using PropertyChanged;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

[AddINotifyPropertyChangedInterface]
public class Message : EntityBase, IMessage
{
	public string MessageContent { get; set; }
	public string OriginalMessage { get; set; }
	public string? AIModel { get; set; }
	public string? ProgrammingLanguage { get; set; }
	public DateTime RunDate { get; set; }
	public bool Favourite { get; set; }
	public bool IsUserMessage => string.IsNullOrEmpty(AIModel);

	// Klucz obcy dla samoodwołania (odpowiedź)
	public int? ReplyToMessageId { get; set; }

	// Właściwość nawigacyjna dla wiadomości, do której odpowiadamy
	[ForeignKey("ReplyToMessageId")]
	public Message? ReplyTo { get; set; }

	// Kolekcja wiadomości będących odpowiedziami na tę wiadomość
	public ICollection<Message> Replies { get; set; } = new List<Message>();
	public Message()
	{
	}
}