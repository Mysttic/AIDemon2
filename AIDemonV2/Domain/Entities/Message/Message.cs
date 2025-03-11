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

	public int? ReplyToMessageId { get; set; }
	[ForeignKey("ReplyToMessageId")]
	public Message? ReplyTo { get; set; }
	public ICollection<Message> Replies { get; set; } = new List<Message>();

	public bool IsModified => ModificationDate > CreationDate;
	public bool IsUserMessage => string.IsNullOrEmpty(ProgrammingLanguage);
	public Message()
	{
	}
}