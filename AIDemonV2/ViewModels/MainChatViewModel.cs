using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace AIDemonV2.ViewModels;

public partial class MainChatViewModel : ObservableObject
{
	public ObservableCollection<Message> Messages { get; } = new();

	[ObservableProperty]
	private string _newMessage = string.Empty;

	public MainChatViewModel()
	{
		LoadInitialMessages();
	}

	private void LoadInitialMessages()
	{
		Messages.Add(new Message
		{
			Id = 1,
			MessageContent = "Hello! How can I help you?",
			OriginalMessage = "User: What can you do?",
			RunDate = DateTime.Now,
			AIModel = new AIModel { Name = "GPT-4" }
		});

		Messages.Add(new Message
		{
			Id = 2,
			MessageContent = "I can assist with various tasks!",
			OriginalMessage = "User: Explain Avalonia UI",
			RunDate = DateTime.Now,
			AIModel = new AIModel { Name = "ChatBot" }
		});
	}

}