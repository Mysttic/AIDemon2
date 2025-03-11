using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDemonV2.ViewModels;

public partial class RightPanelViewModel : ObservableObject
{
	private readonly IMessageRepository _messageRepository;

	private Message _selectedMessage;
	public Message SelectedMessage
	{
		get => _selectedMessage;
		private set => SetProperty(ref _selectedMessage, value);
	}

	public event Action<Message>? MessageUpdated;
	public event Action<string>? ResendMessageRequested;

	[ObservableProperty]
	public string messageContent;

	public RightPanelViewModel(
		IMessageRepository messageRepository)
	{
		_messageRepository = messageRepository;		
	}

	public void SelectMessage(Message message)
	{
		SelectedMessage = message;
		MessageContent = message?.MessageContent ?? string.Empty;
	}

	[RelayCommand]
	private async Task SaveFavourite()
	{
		if (SelectedMessage!= null)
		{
			SelectedMessage.MessageContent = MessageContent;
			SelectedMessage.Favourite = true;
			SelectedMessage.ModificationDate = DateTime.UtcNow;
			await _messageRepository.UpdateAsync(SelectedMessage);
			MessageUpdated?.Invoke(SelectedMessage);
		}
	}

	[RelayCommand]
	private void RunCode()
	{
		if (!string.IsNullOrEmpty(SelectedMessage?.MessageContent))
		{
			Console.WriteLine($"Running code:\n{SelectedMessage?.MessageContent}");
		}
	}

	[RelayCommand]
	private void ResendMessage()
	{
		ResendMessageRequested?.Invoke(MessageContent);
	}

	[RelayCommand]
	private async Task DeleteMessage()
	{
		if (SelectedMessage != null)
		{
			MessageContent = string.Empty;
			SelectedMessage.Favourite = false;
			SelectedMessage.ModificationDate = DateTime.UtcNow;
			SelectedMessage.MessageContent = SelectedMessage.OriginalMessage;
			await _messageRepository.UpdateAsync(SelectedMessage);
			MessageUpdated?.Invoke(SelectedMessage);
			SelectedMessage = null;
		}
	}

	[RelayCommand]
	private async Task ClearMessage()
	{
		if (SelectedMessage != null)
		{
			SelectedMessage = null;
			MessageContent = string.Empty;
		}
	}

}
