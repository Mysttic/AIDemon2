public class MessageDto
{
	public int Id { get; set; }
	public string MessageContent { get; set; }
	public string? OriginalMessage { get; set; }
	public string? AIModel { get; set; }
	public string? ProgrammingLanguage { get; set; }
	public bool Favourite { get; set; }
	public bool IsModified { get; set; }
	public bool IsUserMessage { get; set; }
	public DateTime CreationDate { get; set; }
	public DateTime? ModificationDate { get; set; }
	public List<MessageDto> Replies { get; set; } = new();

	public static MessageDto FromMessage(Message message)
	{
		return new MessageDto
		{
			Id = message.Id,
			MessageContent = message.MessageContent,
			OriginalMessage = message.OriginalMessage,
			AIModel = message.AIModel,
			ProgrammingLanguage = message.ProgrammingLanguage,
			Favourite = message.Favourite,
			IsModified = message.IsModified,
			IsUserMessage = message.IsUserMessage,
			CreationDate = message.CreationDate,
			ModificationDate = message.ModificationDate,
			Replies = message.Replies?.Select(FromMessage).ToList() ?? new List<MessageDto>() // Rekurencyjne mapowanie
		};
	}
}
