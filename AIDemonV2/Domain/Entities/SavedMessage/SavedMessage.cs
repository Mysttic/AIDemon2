using System.ComponentModel.DataAnnotations.Schema;

public class SavedMessage : EntityBase, ISavedMessage
{
	[ForeignKey(nameof(MessageId))]
	public int MessageId { get; set; }
	public Message Message { get; set; }
	public SavedMessage()
	{
	}
}