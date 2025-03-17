using PropertyChanged;
using System.ComponentModel.DataAnnotations.Schema;

[AddINotifyPropertyChangedInterface]
public class Message : EntityBase, IMessage
{
	public string MessageContent { get; set; }
	public string OriginalMessage { get; set; }
	public string? AIModel { get; set; }
	public string? ProgrammingLanguage { get; set; }
	public bool Favourite { get; set; }
	public bool Deleted { get; set; }

	public int? ReplyToMessageId { get; set; }

	[ForeignKey("ReplyToMessageId")]
	public Message? ReplyTo { get; set; }

	public ICollection<Message> Replies { get; set; } = new List<Message>();

	public bool IsModified => ModificationDate > CreationDate;
	public bool IsUserMessage => string.IsNullOrEmpty(ProgrammingLanguage);

	public Message()
	{
	}

	public Message(string messageContent)
	{
		MessageContent = messageContent;
		OriginalMessage = messageContent;
		CreationDate = DateTime.UtcNow;
		ModificationDate = DateTime.UtcNow;
		AIModel = null;
	}
}