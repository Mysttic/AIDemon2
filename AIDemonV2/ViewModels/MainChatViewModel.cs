using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace AIDemonV2.ViewModels;

public partial class MainChatViewModel : ObservableObject
{
	private readonly RightPanelViewModel _rightPanelViewModel;
	private readonly IMessageRepository _messageRepository;
	public event Action? ScrollRequested;
	private ObservableCollection<Message> _messages = new();
	public ObservableCollection<Message> Messages
	{
		get => _messages;
		set
		{
			_messages = value;
			OnPropertyChanged();
		}
	}

	[ObservableProperty]
	private string _newMessage = string.Empty;

	private Message? _selectedMessage;
	public Message? SelectedMessage
	{
		get => _selectedMessage;
		set => SetProperty(ref _selectedMessage, value);
	}


	public MainChatViewModel(
		RightPanelViewModel rightPanelViewModel,
		IMessageRepository messageRepository)
	{
		_rightPanelViewModel = rightPanelViewModel;
		_messageRepository = messageRepository;
		_ = LoadMessages();
	}

	private async Task LoadMessages()
	{
		var messages = await _messageRepository.GetAllAsync();
		Messages.Clear();
		foreach (var message in messages)
		{
			AddMessage(message);
			//Messages.Add(message);
		}
	}

	public void AddMessage(Message message)
	{
		Messages.Add(message);
		ScrollRequested?.Invoke(); // Wywołanie eventu do przewinięcia
	}

	public void RemoveMessage(Message message)
	{
		Messages.Remove(message);
		ScrollRequested?.Invoke();
	}

	//public void SelectMessageForEditing(Message message)
	//{
	//	_rightPanelViewModel.SelectedMessage = message;
	//	//_rightPanelViewModel.CodeEditorText = message.MessageContent;
	//}


}