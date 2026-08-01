using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AIDemon2.ViewModels;

public partial class MainViewModel : ObservableObject
{
	public LeftPanelViewModel LeftPanelViewModel { get; }
	public MainChatViewModel ChatViewModel { get; }
	public RightPanelViewModel RightPanelViewModel { get; }

	private readonly IMessageRepository _messageRepository;
	private readonly IChatService _chatService;
	private readonly ILogger<MainViewModel> _logger;

	// Było zwykłe { get; set; } notyfikujące wyłącznie dzięki weaverowi Fody.
	// Binding IsVisible w MainView.axaml zależy od tego powiadomienia, a jego
	// utrata nie dałaby żadnego błędu kompilacji.
	[ObservableProperty]
	private bool isLoading;

	[ObservableProperty]
	private string newMessageText = string.Empty;

	public MainViewModel(
		LeftPanelViewModel leftPanelViewModel,
		MainChatViewModel chatViewModel,
		RightPanelViewModel rightPanelViewModel,
		IMessageRepository messageRepository,
		IChatService chatService,
		ILogger<MainViewModel> logger)
	{
		LeftPanelViewModel = leftPanelViewModel;
		ChatViewModel = chatViewModel;
		RightPanelViewModel = rightPanelViewModel;
		_messageRepository = messageRepository;
		_chatService = chatService;
		_logger = logger;
		RightPanelViewModel.MessageUpdated += OnMessageUpdated;
		RightPanelViewModel.ResendMessageRequested += ResendMessageRequested;
		LeftPanelViewModel.OnCleanup += OnCleanup;
	}

	/// <summary>
	/// Wczytanie danych wyniesione z konstruktora. Wywoływane po otwarciu okna,
	/// gdzie wyjątek da się złapać i zalogować.
	/// </summary>
	public async Task InitializeAsync()
	{
		await ChatViewModel.InitializeAsync();
		await LeftPanelViewModel.InitializeAsync();
	}

	private async void OnCleanup()
	{
		// async void jest tu nieuniknione (handler zdarzenia), więc wyjątek musi
		// zostać złapany na miejscu — inaczej ubija proces.
		try
		{
			await LoadMessages();
			RightPanelViewModel.SelectMessage(null);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Refreshing the list after clearing the history failed");
		}
	}

	private void OnMessageUpdated(Message? updatedMessage)
	{
		_ = LeftPanelViewModel.LoadFavouriteMessages();
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
		catch (ChatServiceException ex)
		{
			_logger.LogError(ex, "Could not get a reply from the AI service");

			// Komunikat widoczny w rozmowie, ale NIEzapisany do bazy: wcześniej
			// tekst błędu lądował w historii jako pełnoprawna odpowiedź modelu
			// i trafiał do eksportu.
			ChatViewModel.AddMessage(new Message(ex.Message, isUserMessage: false));
		}
		finally
		{
			IsLoading = false;
		}
	}
}