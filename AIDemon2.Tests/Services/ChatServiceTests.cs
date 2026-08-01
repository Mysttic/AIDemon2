using AIDemon2.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace AIDemon2.Tests.Services;

/// <summary>
/// ChatService jest warstwą o największej gęstości defektów: buduje żądanie, tłumaczy
/// błędy i zapisuje odpowiedź do bazy. Testy jadą na prawdziwej bazie SQLite
/// w pamięci i atrapie klienta AI — bez ruchu sieciowego.
/// </summary>
public class ChatServiceTests : IDisposable
{
	private readonly SqliteDbFixture _fixture = new();
	private readonly MessageRepository _messageRepository;
	private readonly SettingsRepository _settingsRepository;

	public ChatServiceTests()
	{
		_messageRepository = new MessageRepository(_fixture);
		_settingsRepository = new SettingsRepository(_fixture);
	}

	private ChatService Utworz(IChatCompletionClient klient, out FakeClientFactory fabryka)
	{
		fabryka = new FakeClientFactory(klient);
		return new ChatService(_messageRepository, _settingsRepository,
			fabryka.Delegat, NullLogger<ChatService>.Instance);
	}

	private ChatService Utworz(IChatCompletionClient klient) => Utworz(klient, out _);

	private async Task UstawKlucz(string? klucz, string? jezyk = null, string? model = "model-x")
	{
		var settings = await _settingsRepository.Get();
		settings!.ApiKey = klucz!;
		settings.ProgrammingLanguage = jezyk;
		settings.AIModel = model;
		settings.InstructionPrompt = "Jestes pomocnym asystentem.";
		await _settingsRepository.UpdateAsync(settings);
	}

	[Fact]
	public async Task SendMessage_Throws_WhenApiKeyMissing()
	{
		// Wcześniej brak klucza kończył się NullReferenceException wewnątrz
		// catch(Exception) i użytkownik widział „problem z połączeniem".
		await UstawKlucz(string.Empty);
		var service = Utworz(new FakeChatCompletionClient());

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => service.SendMessageAsync(new Message("czesc")));

		Assert.Contains("API key", wyjatek.Message);
	}

	[Fact]
	public async Task SendMessage_Throws_ForNullMessage()
	{
		await UstawKlucz("klucz");
		var service = Utworz(new FakeChatCompletionClient());

		await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendMessageAsync(null!));
	}

	[Fact]
	public async Task SendMessage_BuildsTwoSystemMessages_ThenUserMessage()
	{
		await UstawKlucz("klucz", jezyk: "python");
		var klient = new FakeChatCompletionClient();
		var service = Utworz(klient);

		await service.SendMessageAsync(new Message("policz 2+2"));

		var zadanie = Assert.Single(klient.Zadania);
		Assert.Equal(3, zadanie.Messages.Count);
		Assert.Equal(new[] { "system", "system", "user" }, zadanie.Messages.Select(m => m.Role));
		Assert.Contains("python", zadanie.Messages[1].Content);
		// Treść użytkownika jest pakowana w JSON — kontrakt wobec modelu.
		Assert.Equal("policz 2+2",
			JsonDocument.Parse(zadanie.Messages[2].Content).RootElement.GetProperty("text").GetString());
	}

	[Fact]
	public async Task SendMessage_SendsSelectedModel()
	{
		await UstawKlucz("klucz", model: "deepseek-ai/DeepSeek-R1");
		var klient = new FakeChatCompletionClient();
		var service = Utworz(klient);

		await service.SendMessageAsync(new Message("czesc"));

		Assert.Equal("deepseek-ai/DeepSeek-R1", klient.Zadania[0].Model);
	}

	[Fact]
	public async Task SendMessage_StripsThinkTags()
	{
		await UstawKlucz("klucz");
		var service = Utworz(new FakeChatCompletionClient("<think>rozmyslam</think>wynik: 4"));

		var odpowiedz = await service.SendMessageAsync(new Message("2+2"));

		Assert.Equal("wynik: 4", odpowiedz.MessageContent);
	}

	[Fact]
	public async Task SendMessage_TrimsJunkAroundResponse()
	{
		await UstawKlucz("klucz");
		var service = Utworz(new FakeChatCompletionClient("  ---wynik---  "));

		var odpowiedz = await service.SendMessageAsync(new Message("x"));

		Assert.Equal("wynik", odpowiedz.MessageContent);
	}

	[Fact]
	public async Task SendMessage_ThrowsAndPersistsNothing_WhenClientFails()
	{
		// Kluczowa zmiana zachowania: komunikat błędu NIE MOŻE trafić do bazy jako
		// odpowiedź modelu — wcześniej zanieczyszczał historię i eksport.
		await UstawKlucz("klucz");
		var service = Utworz(FakeChatCompletionClient.Rzucajacy(new HttpRequestException("brak sieci")));

		await Assert.ThrowsAsync<ChatServiceException>(
			() => service.SendMessageAsync(new Message("czesc")));

		Assert.Empty(await _messageRepository.GetAllAsync());
	}

	[Fact]
	public async Task SendMessage_KeepsOriginalExceptionAsInner()
	{
		await UstawKlucz("klucz");
		var przyczyna = new HttpRequestException("401");
		var service = Utworz(FakeChatCompletionClient.Rzucajacy(przyczyna));

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => service.SendMessageAsync(new Message("czesc")));

		Assert.Same(przyczyna, wyjatek.InnerException);
	}

	[Fact]
	public async Task SendMessage_PersistsAiMessage_WithReplyToSetToUserMessage()
	{
		await UstawKlucz("klucz", jezyk: "python");
		var service = Utworz(new FakeChatCompletionClient("print(1)"));
		var wiadomoscUzytkownika = await _messageRepository.AddAsync(new Message("napisz skrypt"));

		var odpowiedz = await service.SendMessageAsync(wiadomoscUzytkownika);

		Assert.Equal("print(1)", odpowiedz.OriginalMessage);
		Assert.Equal("python", odpowiedz.ProgrammingLanguage);
		Assert.Equal(wiadomoscUzytkownika.Id, odpowiedz.ReplyToMessageId);
		Assert.Equal(2, (await _messageRepository.GetAllAsync()).Count());
	}

	[Fact]
	public async Task SendMessage_PersistsAiMessage_ThatIsNotMarkedAsModified()
	{
		// ChatService nadpisywał obie daty w inicjalizatorze obiektu, dwoma osobnymi
		// odczytami zegara. Odpowiedź modelu rodziła się więc "zmodyfikowana", a widok
		// rozmowy dokładał pod znacznikiem czasu drugi, wyglądający identycznie.
		await UstawKlucz("klucz", jezyk: "python");
		var service = Utworz(new FakeChatCompletionClient("print(1)"));

		var odpowiedz = await service.SendMessageAsync(new Message("napisz skrypt"));

		Assert.Equal(odpowiedz.CreationDate, odpowiedz.ModificationDate);
		Assert.False(odpowiedz.IsModified);
	}

	[Fact]
	public async Task SendMessage_MarksReplyAsAiAuthored_EvenWithoutProgrammingLanguage()
	{
		// Autor był wyliczany z języka programowania, a ten pochodzi z ustawień.
		// Na świeżej instalacji nikt go jeszcze nie wybrał, więc KAŻDA odpowiedź
		// modelu uchodziła za wiadomość użytkownika — z wyrównaniem do prawej
		// i bez przycisków akcji.
		await UstawKlucz("klucz", jezyk: null);
		var service = Utworz(new FakeChatCompletionClient("odpowiedz"));

		var odpowiedz = await service.SendMessageAsync(new Message("pytanie"));

		Assert.Empty(odpowiedz.ProgrammingLanguage!);
		Assert.False(odpowiedz.IsUserMessage);
	}

	[Fact]
	public async Task SendMessage_Throws_WhenModelNotSelected()
	{
		// Bez modelu żądanie leciało do API z pustym polem "model" i wracało
		// nieczytelnym błędem HTTP.
		await UstawKlucz("klucz", model: null);
		var service = Utworz(new FakeChatCompletionClient());

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => service.SendMessageAsync(new Message("czesc")));

		Assert.Contains("AI model", wyjatek.Message);
	}

	[Fact]
	public async Task SendMessage_SkipsSystemMessage_WhenInstructionPromptEmpty()
	{
		await UstawKlucz("klucz", jezyk: "python");
		var settings = await _settingsRepository.Get();
		settings!.InstructionPrompt = "   ";
		await _settingsRepository.UpdateAsync(settings);
		var klient = new FakeChatCompletionClient();
		var service = Utworz(klient);

		await service.SendMessageAsync(new Message("czesc"));

		var zadanie = Assert.Single(klient.Zadania);
		Assert.Equal(new[] { "system", "user" }, zadanie.Messages.Select(m => m.Role));
	}

	[Fact]
	public async Task ResetClient_ForcesClientRecreation_OnNextSend()
	{
		// Regresja: ResetClient ustawiał wyłącznie flagę wiadomości systemowych,
		// więc po zmianie klucza API aplikacja do restartu używała starego klienta.
		await UstawKlucz("stary-klucz");
		var service = Utworz(new FakeChatCompletionClient(), out var fabryka);
		await service.SendMessageAsync(new Message("pierwsza"));
		Assert.Equal(1, fabryka.LiczbaUtworzen);

		await UstawKlucz("nowy-klucz");
		service.ResetClient();
		await service.SendMessageAsync(new Message("druga"));

		Assert.Equal(2, fabryka.LiczbaUtworzen);
		Assert.Equal(new[] { "stary-klucz", "nowy-klucz" }, fabryka.UzyteKlucze);
	}

	[Fact]
	public async Task SendMessage_ReusesClient_WithoutReset()
	{
		await UstawKlucz("klucz");
		var service = Utworz(new FakeChatCompletionClient(), out var fabryka);

		await service.SendMessageAsync(new Message("pierwsza"));
		await service.SendMessageAsync(new Message("druga"));

		Assert.Equal(1, fabryka.LiczbaUtworzen);
	}

	public void Dispose() => _fixture.Dispose();
}
