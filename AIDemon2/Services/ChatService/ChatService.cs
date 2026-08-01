using AIDemon2.Services.ChatService;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

public class ChatService : IChatService
{
	private readonly IMessageRepository _messageRepository;
	private readonly ISettingsRepository _settingsRepository;
	private readonly Func<string, IChatCompletionClient> _clientFactory;
	private readonly ILogger<ChatService> _logger;

	private IChatCompletionClient? _client;
	private Settings? _settings;
	private bool _systemMessagesRequired = true;

	/// <param name="clientFactory">
	/// Fabryka klienta zamiast tworzenia go w środku metody — dzięki temu test
	/// podstawia własną implementację i nie wychodzi w sieć, a zmiana dostawcy
	/// modeli (io.net -> OpenRouter) nie dotknęła tej klasy.
	/// </param>
	public ChatService(
		IMessageRepository messageRepository,
		ISettingsRepository settingsRepository,
		Func<string, IChatCompletionClient> clientFactory,
		ILogger<ChatService> logger)
	{
		_messageRepository = messageRepository;
		_settingsRepository = settingsRepository;
		_clientFactory = clientFactory;
		_logger = logger;
	}

	private async Task InitializeAsync()
	{
		// Wcześniej brak ustawień kończył się NullReferenceException wewnątrz bloku
		// catch(Exception), który zamieniał go na „problem z połączeniem".
		_settings = await _settingsRepository.Get()
			?? throw new ChatServiceException(
				"Application settings are missing from the database, so no message can be sent.");

		if (string.IsNullOrWhiteSpace(_settings.ApiKey))
			throw new ChatServiceException(
				"No API key is set. Add one in the application settings.");

		// Bez modelu żądanie leciało do API z pustym polem "model" i wracało
		// nieczytelnym błędem HTTP. Lepiej powiedzieć wprost, czego brakuje.
		if (string.IsNullOrWhiteSpace(_settings.AIModel))
			throw new ChatServiceException(
				"No AI model is selected. Pick one in the application settings.");

		_client = _clientFactory(_settings.ApiKey);
	}

	public async Task<Message> SendMessageAsync(Message userMessage)
	{
		if (userMessage == null)
			throw new ArgumentNullException(nameof(userMessage));

		if (_client == null)
			await InitializeAsync();

		var settings = _settings!;

		// Przygotowanie wiadomości dla AI
		var messages = new List<OpenRouterMessage>();

		// Jeśli flaga _systemMessagesRequired jest ustawiona, dodaj dwie wiadomości systemowe
		if (_systemMessagesRequired)
		{
			// Pusta instrukcja to nie to samo co brak instrukcji — wysyłanie
			// wiadomości systemowej bez treści tylko zużywa tokeny.
			if (!string.IsNullOrWhiteSpace(settings.InstructionPrompt))
				messages.Add(OpenRouterMessage.System(settings.InstructionPrompt));
			messages.Add(OpenRouterMessage.System(
				"For script writing use programming language: " + settings.ProgrammingLanguage));
			//_systemMessagesRequired = false; //odznaczyć jeśli chcemy aby instrukcje były wysyłane tylko raz
		}

		messages.Add(OpenRouterMessage.User(
			JsonSerializer.Serialize(new { text = userMessage.MessageContent })));

		var chatRequest = new OpenRouterChatRequest
		{
			Model = settings.AIModel!, // niepuste — sprawdzone w InitializeAsync
			Messages = messages
		};

		string responseText;
		try
		{
			responseText = await GetResponseFromAIAsync(chatRequest);
		}
		catch (ChatServiceException ex)
		{
			// Klient OpenRoutera zna powód (zły klucz, brak środków, limit zapytań)
			// i ma gotowy komunikat. Owijanie go w ogólnik "sprawdź klucz i sieć"
			// gubiłoby tę informację — logujemy i przepuszczamy bez zmian.
			_logger.LogError(ex, "Call to the AI service failed (model {Model})", settings.AIModel);
			throw;
		}
		catch (Exception ex)
		{
			// Wcześniej ten blok połykał KAŻDY wyjątek i zapisywał komunikat błędu do bazy
			// jako odpowiedź AI — nie do odróżnienia od prawdziwej, również w eksporcie.
			_logger.LogError(ex, "Call to the AI service failed (model {Model})", settings.AIModel);

			throw new ChatServiceException(
				"Could not reach the AI service. Check your API key and network connection.", ex);
		}

		var aiMessage = new Message
		{
			MessageContent = responseText,
			OriginalMessage = responseText,
			CreationDate = DateTime.UtcNow,
			ModificationDate = DateTime.UtcNow,
			AIModel = settings.AIModel,
			ProgrammingLanguage = string.IsNullOrEmpty(settings.ProgrammingLanguage) ? string.Empty : settings.ProgrammingLanguage,
			IsUserMessage = false,
			// Klucz obcy, NIE nawigacja: repozytorium tworzy nowy kontekst na każdą
			// operację, a Add() po nawigacji potraktowałby odłączoną wiadomość
			// użytkownika jako nową i wstawił ją drugi raz.
			//
			// Id == 0 oznacza wiadomość jeszcze niezapisaną. Wywołujący ma ją zapisać
			// przed wysłaniem (tak robi MainViewModel); gdy tego nie zrobił, zostawiamy
			// powiązanie puste zamiast wstawiać klucz obcy wskazujący na nieistniejący wiersz.
			ReplyToMessageId = userMessage.Id != 0 ? userMessage.Id : null
		};

		await _messageRepository.AddAsync(aiMessage);
		return aiMessage;
	}

	/// <summary>
	/// Wymusza odtworzenie klienta i ponowne wczytanie ustawień przy następnej wiadomości.
	///
	/// Wcześniej metoda ustawiała wyłącznie <c>_systemMessagesRequired</c>, więc po zmianie
	/// klucza API aplikacja do restartu wysyłała żądania starym kluczem, a użytkownik
	/// widział jedynie komunikat o problemie z połączeniem.
	/// </summary>
	public void ResetClient()
	{
		// Każdy klient trzyma własny HttpClient; bez zwolnienia zmiana klucza API
		// zostawiałaby po sobie gniazdo aż do zebrania przez GC.
		(_client as IDisposable)?.Dispose();
		_client = null;
		_settings = null;
		_systemMessagesRequired = true;
	}

	private async Task<string> GetResponseFromAIAsync(OpenRouterChatRequest chatRequest)
	{
		var content = await _client!.CompleteAsync(chatRequest);
		return SanitizeResponse(content);
	}

	private string SanitizeResponse(string response)
	{
		var cleaned = Regex.Replace(response, @"<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		cleaned = Regex.Replace(cleaned, @"^[\s\u200B\p{C}\|%-]+|[\s\u200B\p{C}\|%-]+$", "");
		return cleaned.Trim();
	}
}