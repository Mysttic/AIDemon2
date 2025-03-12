using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AIDemonV2.ViewModels;

public partial class MainChatViewModel : ObservableObject
{
	private readonly IChatService _chatService;
	private readonly IMessageRepository _messageRepository;

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
		IMessageRepository messageRepository)
	{
		_chatService = chatService;
		_messageRepository = messageRepository;
		_ = LoadMessages();
	}

	public async Task LoadMessages()
	{
		var messages = await _messageRepository.GetMessages();
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