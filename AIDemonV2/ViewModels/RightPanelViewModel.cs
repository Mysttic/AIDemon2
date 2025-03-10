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

	[ObservableProperty]
	public Message selectedMessage;

	public event Action<Message>? MessageUpdated;

	public RightPanelViewModel(
		IMessageRepository messageRepository)
	{
		_messageRepository = messageRepository;
	}

	[RelayCommand]
	private async Task SaveFavourite()
	{
		if (SelectedMessage!= null)
		{
			SelectedMessage.Favourite = true;
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
	private async Task DeleteMessage()
	{
		if (SelectedMessage != null)
		{
			SelectedMessage.Favourite = false;
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
		}
	}

}
