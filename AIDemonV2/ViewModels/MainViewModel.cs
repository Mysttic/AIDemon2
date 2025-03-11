using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using IoIntelligence.Client.Models.AIModel.Chat;
using IoIntelligence.Client.Interfaces;
using IoIntelligence.Client.Services;
using System.Text.RegularExpressions;
using Tmds.DBus.Protocol;

namespace AIDemonV2.ViewModels;

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
		_ = LoadMessages();
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
		if (string.IsNullOrWhiteSpace(NewMessageText)) return;
		IsLoading = true;
		try
		{
			var aiMessage = await _chatService.SendMessageAsync(NewMessageText);
			ChatViewModel.AddMessage(aiMessage);
			NewMessageText = string.Empty;
		}
		finally
		{
			IsLoading = false;
		}
	}
}
