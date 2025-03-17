public interface IMessage : IEntityBase
{
	public string MessageContent { get; set; }
	public string OriginalMessage { get; set; }
	public string? AIModel { get; set; }
	public string? ProgrammingLanguage { get; set; }
	public bool Favourite { get; set; }
	public bool Deleted { get; set; }
}