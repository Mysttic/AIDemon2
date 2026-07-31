using IoIntelligence.Client.Models.AIModel.Chat;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

public class ChatService : IChatService
{
	private readonly IMessageRepository _messageRepository;
	private readonly ISettingsRepository _settingsRepository;
	private readonly Func<string, IChatCompletionClient> _clientFactory;
	private readonly ILogger<ChatService> _logger;

	private IChatCompletionClient? _ioIntelligenceClient;
	private Settings? _settings;
	private bool _systemMessagesRequired = true;

	/// <param name="clientFactory">
	/// Fabryka klienta zamiast <c>new IoIntelligenceClient(...)</c> w środku metody.
	/// Typem jest własny <see cref="IChatCompletionClient"/>, a nie interfejs z pakietu:
	/// ten drugi wystawia klasę ModelClient z niewirtualną metodą, więc nie dałoby się
	/// podstawić odpowiedzi i każdy test wychodziłby w sieć.
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
				"Brak ustawień aplikacji w bazie — nie da się wysłać wiadomości.");

		if (string.IsNullOrWhiteSpace(_settings.ApiKey))
			throw new ChatServiceException(
				"Nie ustawiono klucza API. Uzupełnij go w ustawieniach aplikacji.");

		// Bez modelu żądanie leciało do API z pustym polem "model" i wracało
		// nieczytelnym błędem HTTP. Lepiej powiedzieć wprost, czego brakuje.
		if (string.IsNullOrWhiteSpace(_settings.AIModel))
			throw new ChatServiceException(
				"Nie wybrano modelu AI. Wskaż go w ustawieniach aplikacji.");

		_ioIntelligenceClient = _clientFactory(_settings.ApiKey);
	}

	public async Task<Message> SendMessageAsync(Message userMessage)
	{
		if (userMessage == null)
			throw new ArgumentNullException(nameof(userMessage));

		if (_ioIntelligenceClient == null)
			await InitializeAsync();

		var settings = _settings!;

		// Przygotowanie wiadomości dla AI
		var messages = new List<ChatCompletionMessage>();

		// Jeśli flaga _systemMessagesRequired jest ustawiona, dodaj dwie wiadomości systemowe
		if (_systemMessagesRequired)
		{
			// Pusta instrukcja to nie to samo co brak instrukcji — wysyłanie
			// wiadomości systemowej bez treści tylko zużywa tokeny.
			if (!string.IsNullOrWhiteSpace(settings.InstructionPrompt))
				messages.Add(new ChatCompletionMessage
				{
					Role = "system",
					Content = settings.InstructionPrompt
				});
			messages.Add(new ChatCompletionMessage
			{
				Role = "system",
				Content = "For script writing use programming language: " + settings.ProgrammingLanguage
			});
			//_systemMessagesRequired = false; //odznaczyć jeśli chcemy aby instrukcje były wysyłane tylko raz
		}

		messages.Add(new ChatCompletionMessage
		{
			Role = "user",
			Content = JsonSerializer.Serialize(new { text = userMessage.MessageContent })
		});

		var chatRequest = new ChatCompletionRequest
		{
			Model = settings.AIModel!, // niepuste — sprawdzone w InitializeAsync
			Messages = messages
		};

		string responseText;
		try
		{
			responseText = await GetResponseFromAIAsync(chatRequest);
		}
		catch (Exception ex)
		{
			// Wcześniej ten blok połykał KAŻDY wyjątek i zapisywał komunikat błędu do bazy
			// jako odpowiedź AI — nie do odróżnienia od prawdziwej, również w eksporcie.
			// Teraz błąd jest logowany i przekazywany wyżej, a warstwa widoku decyduje,
			// jak go pokazać użytkownikowi.
			_logger.LogError(ex, "Wywołanie usługi AI nie powiodło się (model {Model})", settings.AIModel);

			throw new ChatServiceException(
				"Nie udało się połączyć z usługą AI. Sprawdź klucz API i połączenie z siecią.", ex);
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
		_ioIntelligenceClient = null;
		_settings = null;
		_systemMessagesRequired = true;
	}

	private async Task<string> GetResponseFromAIAsync(ChatCompletionRequest chatRequest)
	{
		var content = await _ioIntelligenceClient!.CompleteAsync(chatRequest);
		return SanitizeResponse(content);
	}

	private string SanitizeResponse(string response)
	{
		var cleaned = Regex.Replace(response, @"<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		cleaned = Regex.Replace(cleaned, @"^[\s\u200B\p{C}\|%-]+|[\s\u200B\p{C}\|%-]+$", "");
		return cleaned.Trim();
	}
}