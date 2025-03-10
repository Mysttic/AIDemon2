using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace AIDemonV2.ViewModels;

public partial class MainViewModel : ObservableObject
{
	public LeftPanelViewModel LeftPanelViewModel { get; }
	public MainChatViewModel ChatViewModel { get; }
	public RightPanelViewModel RightPanelViewModel { get; }

	private readonly IMessageRepository _messageRepository;
	private readonly ISettingsRepository _settingsRepository;
	private readonly HttpClient _httpClient;

	[ObservableProperty]
	private string newMessageText = string.Empty;

	public MainViewModel(LeftPanelViewModel leftPanelViewModel, MainChatViewModel chatViewModel, 
		IMessageRepository messageRepository, ISettingsRepository settingsRepository)
	{
		LeftPanelViewModel = leftPanelViewModel;
		ChatViewModel = chatViewModel;
		RightPanelViewModel = new RightPanelViewModel();
		_messageRepository = messageRepository;
		_settingsRepository = settingsRepository;
		_httpClient = new HttpClient();
		_ = LoadMessages();
	}

	private async Task LoadMessages()
	{
		var messages = await _messageRepository.GetAllAsync();
		ChatViewModel.Messages.Clear();
		foreach (var message in messages)
		{
			ChatViewModel.Messages.Add(message);
		}
	}

	[RelayCommand]
	private async Task SendMessage()
	{
		Settings? settings = await _settingsRepository.Get();
		if (string.IsNullOrWhiteSpace(NewMessageText)) return;

		var userMessage = new Message
		{
			MessageContent = NewMessageText,
			OriginalMessage = $"User: {NewMessageText}",
			RunDate = DateTime.UtcNow,
			CreationDate = DateTime.UtcNow,
			ModificationDate = DateTime.UtcNow,
			AIModel = null // Oznacza wiadomość użytkownika
		};

		await _messageRepository.AddAsync(userMessage);
		ChatViewModel.AddMessage(userMessage);
		var requestBody = JsonSerializer.Serialize(new { text = NewMessageText });
		NewMessageText = string.Empty; // Wyczyść pole wejściowe

		try
		{
			var request = new HttpRequestMessage(HttpMethod.Post, settings.ApiUrl)
			{
				Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
			};

			// Dodaj nagłówki (np. klucz API)
			request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");

			var response = await _httpClient.SendAsync(request);
			if (response.IsSuccessStatusCode)
			{
				var responseText = await response.Content.ReadAsStringAsync();
				await HandleAIResponse(settings, responseText);
			}
			else
			{
				Console.WriteLine($"Błąd API: {response.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Błąd HTTP: {ex.Message}");
		}
	}

	private async Task HandleAIResponse(Settings settings, string responseText)
	{
		try
		{
			var aiMessage = new Message
			{
				MessageContent = responseText,
				OriginalMessage = responseText,
				CreationDate = DateTime.UtcNow,
				ModificationDate = DateTime.UtcNow,
				AIModel = settings.AIModel
			};

			await _messageRepository.AddAsync(aiMessage);
			ChatViewModel.AddMessage(aiMessage);
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}


}
