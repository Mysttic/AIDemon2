using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AIDemon2.ViewModels;

public partial class MainChatViewModel : ObservableObject
{
	private readonly IChatService _chatService;
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
		IChatService chatService,
		IMessageRepository messageRepository)
	{
		_chatService = chatService;
		_messageRepository = messageRepository;
	}

	/// <summary>
	/// Wczytanie danych wyniesione z konstruktora. Zapis "_ = LoadMessages()" oznaczał,
	/// że konstruktor sięgał do bazy, a ewentualny wyjątek nie miał gdzie wypłynąć.
	/// Uniemożliwiał też utworzenie ViewModelu w teście bez działającej bazy.
	/// </summary>
	public Task InitializeAsync() => LoadMessages();

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