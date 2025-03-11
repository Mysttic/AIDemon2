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
	private readonly ISettingsRepository _settingsRepository;
	private IIoIntelligenceClient _ioIntelligenceClient;
	private Settings _settings;

	public static bool clientWasInitialized = false;
	public bool IsLoading { get; set; }

	[ObservableProperty]
	private string newMessageText = string.Empty;

	public MainViewModel(
		LeftPanelViewModel leftPanelViewModel, 
		MainChatViewModel chatViewModel, 
		RightPanelViewModel rightPanelViewModel,
		IMessageRepository messageRepository, 
		ISettingsRepository settingsRepository)
	{
		LeftPanelViewModel = leftPanelViewModel;
		ChatViewModel = chatViewModel;
		RightPanelViewModel = rightPanelViewModel;
		_messageRepository = messageRepository;
		_settingsRepository = settingsRepository;
		RightPanelViewModel.MessageUpdated += OnMessageUpdated;
		_ = LoadMessages();
		_ = CreateIOInteligenceClient();
	}

	private void OnMessageUpdated(Message? updatedMessage)
	{
		_ = LeftPanelViewModel.LoadFavouriteMessages();
	}


	private async Task CreateIOInteligenceClient()
	{
		_settings = await _settingsRepository.Get();
		_ioIntelligenceClient = new IoIntelligenceClient(_settings.ApiKey);
	}

	private async Task LoadMessages()
	{
		var messages = await _messageRepository.GetAllAsync();
		ChatViewModel.Messages.Clear();
		foreach (var message in messages.OrderBy(m=>m.Id))
		{
			ChatViewModel.Messages.Add(message);
		}
	}

	[RelayCommand]
	private async Task SendMessage()
	{
		if (string.IsNullOrWhiteSpace(NewMessageText)) return;

		var userMessage = new Message
		{
			MessageContent = NewMessageText,
			OriginalMessage = NewMessageText,
			RunDate = DateTime.UtcNow,
			CreationDate = DateTime.UtcNow,
			ModificationDate = DateTime.UtcNow,
			AIModel = null // Oznacza wiadomość użytkownika
		};

		await _messageRepository.AddAsync(userMessage);
		ChatViewModel.AddMessage(userMessage);
		var requestBody = JsonSerializer.Serialize(new { text = NewMessageText });
		NewMessageText = string.Empty; // Wyczyść pole wejściowe

		List<ChatCompletionMessage> Messages = new List<ChatCompletionMessage>();
		if(!clientWasInitialized)
			Messages.AddRange(new List<ChatCompletionMessage>
			{
				new ChatCompletionMessage
				{
					Role = "system",
					Content = _settings.InstructionPrompt
				},
				new ChatCompletionMessage
				{
					Role = "system",
					Content = "For script writing use programming language: "+_settings.ProgrammingLanguage
				}
			});
		Messages.Add(new ChatCompletionMessage
		{
			Role = "user",
			Content = requestBody
		});
		
		var chatRequest = new ChatCompletionRequest
		{
			Model = _settings.AIModel,
			Messages = Messages

		};

		try
		{
			IsLoading = true;
			var chatResponse = await _ioIntelligenceClient.Models.CreateChatCompletionAsync(chatRequest);
			string chatReply = SanitizeResponse(chatResponse.Choices.First().Message.Content);
			await HandleAIResponse(_settings, userMessage, chatReply);
		}
		catch (Exception ex)
		{
			await HandleAIResponse(_settings, userMessage, "Sorry, I'm having trouble connecting to the AI service. Please try again later.", true);
		}
		finally
		{
			IsLoading = false;
		}
	}

	static string SanitizeResponse(string response)
	{
		// Remove any <think> blocks and trim extra characters.
		var cleaned = Regex.Replace(response, @"<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		cleaned = Regex.Replace(cleaned, @"^[\s\u200B\p{C}\|%-]+|[\s\u200B\p{C}\|%-]+$", "");
		return cleaned.Trim();
	}

	private async Task HandleAIResponse(Settings settings, Message userMessage, string responseText, bool isFailed = false)
	{
		var aiMessage = new Message
		{
			MessageContent = responseText,
			OriginalMessage = responseText,
			CreationDate = DateTime.UtcNow,
			ModificationDate = DateTime.UtcNow,
			AIModel = settings?.AIModel,
			ProgrammingLanguage = isFailed ? string.Empty : settings?.ProgrammingLanguage,
			ReplyTo = userMessage,
		};

		await _messageRepository.AddAsync(aiMessage);
		ChatViewModel.AddMessage(aiMessage);
	}
}
