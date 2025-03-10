﻿using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.ComponentModel.DataAnnotations.Schema;

public class Message : EntityBase, IMessage
{
	public string MessageContent { get; set; }
	public string OriginalMessage { get; set; }	
	public string? AIModel { get; set; }
	public string? ProgrammingLanguage { get; set; }
	public DateTime RunDate { get; set; }
	public bool Favourite { get; set; }
	public bool IsUserMessage => string.IsNullOrEmpty(AIModel);
	public Message()
	{
	}
}