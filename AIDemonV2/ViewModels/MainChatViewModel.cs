using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace AIDemonV2.ViewModels;

public partial class MainChatViewModel : ObservableObject
{
	private readonly IChatService _chatService;
	private readonly IMessageRepository _messageRepository;
	private readonly RightPanelViewModel _rightPanelViewModel;

	public event Action? ScrollRequested;
	public event Action<bool>? IsLoading;
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
		IChatService chatService,
		IMessageRepository messageRepository,
		RightPanelViewModel rightPanelViewModel)
	{
		_chatService = chatService;
		_messageRepository = messageRepository;
		_rightPanelViewModel = rightPanelViewModel;
		_rightPanelViewModel.ResendMessageRequested += ResendMessageRequested;
		_ = LoadMessages();
	}

	private async void ResendMessageRequested(Message newMessage)
	{
		NewMessage = newMessage.MessageContent;
		if (string.IsNullOrWhiteSpace(NewMessage))
			return;

		AddMessage(newMessage);

		IsLoading?.Invoke(true);
		// Wyślij wiadomość przez serwis, który odpowiada za komunikację z AI
		var message = await _chatService.SendMessageAsync(NewMessage);

		// Dodaj wiadomość do kolekcji, co odświeży widok
		AddMessage(message);

		// Wyczyść pole wejściowe
		NewMessage = string.Empty;
		IsLoading?.Invoke(false);
	}

	public async Task LoadMessages()
	{
		var messages = await _messageRepository.GetAllAsync();
		Messages.Clear();
		foreach (var message in messages)
		{
			AddMessage(message);
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

}