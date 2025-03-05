using System;

public interface IMessage : IEntityBase
{
	public string MessageContent { get; set; }
	public string OriginalMessage { get; set; }
	public AIModel AIModel { get; set; }
	public DateTime RunDate { get; set; }
}

