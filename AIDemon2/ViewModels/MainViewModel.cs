using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIDemon2.ViewModels;

public partial class MainViewModel : ObservableObject
{
	public LeftPanelViewModel LeftPanelViewModel { get; }
	public MainChatViewModel ChatViewModel { get; }
	public RightPanelViewModel RightPanelViewModel { get; }

	private readonly IMessageRepository _messageRepository;
	private readonly IChatService _chatService;

	public bool IsLoading { get; set; }

	[ObservableProperty]
	private string newMessageText = string.Empty;

	public MainViewModel(
		LeftPanelViewModel leftPanelViewModel,
		MainChatViewModel chatViewModel,
		RightPanelViewModel rightPanelViewModel,
		IMessageRepository messageRepository,
		IChatService chatService)
	{
		LeftPanelViewModel = leftPanelViewModel;
		ChatViewModel = chatViewModel;
		RightPanelViewModel = rightPanelViewModel;
		_messageRepository = messageRepository;
		_chatService = chatService;
		RightPanelViewModel.MessageUpdated += OnMessageUpdated;
		RightPanelViewModel.ResendMessageRequested += ResendMessageRequested;
		ChatViewModel.IsLoading += OnIsLoading;
		LeftPanelViewModel.OnCleanup += OnCleanup;
		_ = LoadMessages();
	}

	private async void OnCleanup()
	{
		await LoadMessages();
		RightPanelViewModel.SelectMessage(null);
	}

	private void OnMessageUpdated(Message? updatedMessage)
	{
		_ = LeftPanelViewModel.LoadFavouriteMessages();
	}

	private void OnIsLoading(bool isLoading)
	{
		IsLoading = isLoading;
	}

	private void ResendMessageRequested(string newMessage)
	{
		NewMessageText = newMessage;
	}

	private async Task LoadMessages()
	{
		await ChatViewModel.LoadMessages();
	}

	[RelayCommand]
	private async Task SendMessage()
	{
		if (string.IsNullOrWhiteSpace(NewMessageText))
			return;
		Message userMessage = new Message(NewMessageText);
		await _messageRepository.AddAsync(userMessage);
		ChatViewModel.AddMessage(userMessage);
		NewMessageText = string.Empty;

		try
		{
			IsLoading = true;
			var aiMessage = await _chatService.SendMessageAsync(userMessage);
			ChatViewModel.AddMessage(aiMessage);
		}
		finally
		{
			IsLoading = false;
		}
	}
}