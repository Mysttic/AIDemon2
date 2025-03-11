using System.Text.Json;
using System.Text.RegularExpressions;
using IoIntelligence.Client.Models.AIModel.Chat;
using IoIntelligence.Client.Interfaces;
using IoIntelligence.Client.Services;

public class ChatService : IChatService
{
	private readonly IMessageRepository _messageRepository;
	private readonly ISettingsRepository _settingsRepository;
	private IIoIntelligenceClient _ioIntelligenceClient;
	private Settings _settings;
	private bool _systemMessagesRequired = true;

	public ChatService(IMessageRepository messageRepository, ISettingsRepository settingsRepository)
	{
		_messageRepository = messageRepository;
		_settingsRepository = settingsRepository;
	}

	private async Task InitializeAsync()
	{
		_settings = await _settingsRepository.Get();
		_ioIntelligenceClient = new IoIntelligenceClient(_settings.ApiKey);
	}

	public async Task<Message> SendMessageAsync(string newMessageText)
	{
		if (string.IsNullOrWhiteSpace(newMessageText))
			throw new ArgumentException("Message text cannot be empty.", nameof(newMessageText));

		if (_ioIntelligenceClient == null)
			await InitializeAsync();

		// Utwórz wiadomość użytkownika
		var userMessage = new Message
		{
			MessageContent = newMessageText,
			OriginalMessage = newMessageText,
			RunDate = DateTime.UtcNow,
			CreationDate = DateTime.UtcNow,
			ModificationDate = DateTime.UtcNow,
			AIModel = null
		};

		await _messageRepository.AddAsync(userMessage);

		// Przygotowanie wiadomości dla AI
		var messages = new List<ChatCompletionMessage>();

		// Jeśli flaga _systemMessagesRequired jest ustawiona, dodaj dwie wiadomości systemowe
		if (_systemMessagesRequired)
		{
			messages.Add(new ChatCompletionMessage
			{
				Role = "system",
				Content = _settings.InstructionPrompt
			});
			messages.Add(new ChatCompletionMessage
			{
				Role = "system",
				Content = "For script writing use programming language: " + _settings.ProgrammingLanguage
			});
			//_systemMessagesRequired = false; //odznaczyć jeśli chcemy aby instrukcje były wysyłane tylko raz
		}

		messages.Add(new ChatCompletionMessage
		{
			Role = "user",
			Content = JsonSerializer.Serialize(new { text = newMessageText })
		});

		var chatRequest = new ChatCompletionRequest
		{
			Model = _settings.AIModel,
			Messages = messages
		};

		string responseText;
		try
		{
			responseText = await GetResponseFromAIAsync(chatRequest);
		}
		catch (Exception)
		{
			responseText = "Sorry, I'm having trouble connecting to the AI service. Please try again later.";
		}

		var aiMessage = new Message
		{
			MessageContent = responseText,
			OriginalMessage = responseText,
			CreationDate = DateTime.UtcNow,
			ModificationDate = DateTime.UtcNow,
			AIModel = _settings.AIModel,
			ProgrammingLanguage = string.IsNullOrEmpty(_settings.ProgrammingLanguage) ? string.Empty : _settings.ProgrammingLanguage,
			ReplyTo = userMessage
		};

		await _messageRepository.AddAsync(aiMessage);
		return aiMessage;
	}

	public void ResetClient()
	{
		_systemMessagesRequired = true;
	}

	private async Task<string> GetResponseFromAIAsync(ChatCompletionRequest chatRequest)
	{
		var chatResponse = await _ioIntelligenceClient.Models.CreateChatCompletionAsync(chatRequest);
		return SanitizeResponse(chatResponse.Choices.First().Message.Content);
	}

	private string SanitizeResponse(string response)
	{
		var cleaned = Regex.Replace(response, @"<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		cleaned = Regex.Replace(cleaned, @"^[\s\u200B\p{C}\|%-]+|[\s\u200B\p{C}\|%-]+$", "");
		return cleaned.Trim();
	}
}
