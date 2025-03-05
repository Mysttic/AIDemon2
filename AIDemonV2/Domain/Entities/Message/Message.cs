using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.ComponentModel.DataAnnotations.Schema;

public class Message : EntityBase, IMessage
{
	public string MessageContent { get; set; }
	public string OriginalMessage { get; set; }
	[ForeignKey(nameof(AIModelId))]
	public int AIModelId { get; set; }
	public AIModel? AIModel { get; set; }
	public DateTime RunDate { get; set; }
	public Message()
	{
	}
}