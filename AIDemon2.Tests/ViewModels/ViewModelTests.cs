using AIDemon2.Tests.Infrastructure;
using AIDemon2.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIDemon2.Tests.ViewModels;

/// <summary>
/// Testy warstwy widoku od strony ViewModeli. Możliwe dopiero po wyniesieniu
/// wczytywania danych z konstruktorów — wcześniej samo utworzenie ViewModelu
/// sięgało do bazy.
///
/// Widoki są świadomie poza zakresem: pobierają zależności przez
/// Application.Current.Resources["Services"], czego nie da się rozsądnie
/// odtworzyć bez uruchomionej aplikacji Avalonii.
/// </summary>
public class ViewModelTests : IDisposable
{
	private readonly SqliteDbFixture _fixture = new();
	private readonly MessageRepository _messages;
	private readonly SettingsRepository _settings;
	private readonly FakeDialogService _dialogi = new();
	private readonly FakeCodeRunnerService _runner = new();
	private readonly FakeMessageExportService _eksport = new();

	public ViewModelTests()
	{
		_messages = new MessageRepository(_fixture);
		_settings = new SettingsRepository(_fixture);
	}

	private RightPanelViewModel PrawyPanel() =>
		// uiPost wykonywany od razu: Dispatcher.UIThread.Post bez aplikacji Avalonii
		// nie rzuca, tylko kolejkuje w próżnię, więc asercja widziałaby pustą konsolę.
		new(_messages, _runner, _eksport, _dialogi, akcja => akcja());

	private LeftPanelViewModel LewyPanel() =>
		new(_messages, _dialogi, _eksport);

	private MainViewModel Glowny(IChatService chat) =>
		new(LewyPanel(), new MainChatViewModel(chat, _messages), PrawyPanel(),
			_messages, chat, NullLogger<MainViewModel>.Instance);

	private ChatService Chat(IChatCompletionClient klient) =>
		new(_messages, _settings, _ => klient, NullLogger<ChatService>.Instance);

	private async Task UstawKlucz()
	{
		var s = await _settings.Get();
		s!.ApiKey = "klucz";
		s.AIModel = "model";
		s.InstructionPrompt = "instrukcja";
		await _settings.UpdateAsync(s);
	}

	// ---------- MainViewModel ----------

	[Fact]
	public async Task SendMessage_DoesNothing_ForWhitespaceInput()
	{
		var vm = Glowny(Chat(new FakeChatCompletionClient()));
		vm.NewMessageText = "   ";

		await vm.SendMessageCommand.ExecuteAsync(null);

		Assert.Empty(await _messages.GetAllAsync());
	}

	[Fact]
	public async Task SendMessage_PersistsUserMessage_ThenAppendsAiReply()
	{
		await UstawKlucz();
		var vm = Glowny(Chat(new FakeChatCompletionClient("odpowiedz AI")));
		vm.NewMessageText = "pytanie";

		await vm.SendMessageCommand.ExecuteAsync(null);

		Assert.Equal(2, vm.ChatViewModel.Messages.Count);
		Assert.Equal("pytanie", vm.ChatViewModel.Messages[0].MessageContent);
		Assert.Equal("odpowiedz AI", vm.ChatViewModel.Messages[1].MessageContent);
		Assert.Equal(2, (await _messages.GetAllAsync()).Count());
	}

	[Fact]
	public async Task SendMessage_ClearsInput()
	{
		await UstawKlucz();
		var vm = Glowny(Chat(new FakeChatCompletionClient()));
		vm.NewMessageText = "pytanie";

		await vm.SendMessageCommand.ExecuteAsync(null);

		Assert.Equal(string.Empty, vm.NewMessageText);
	}

	[Fact]
	public async Task SendMessage_ShowsErrorInChat_ButDoesNotPersistIt()
	{
		// Komunikat błędu ma być widoczny w rozmowie, ale NIE w bazie — inaczej
		// zanieczyszcza historię i eksport, udając prawdziwą odpowiedź modelu.
		await UstawKlucz();
		var vm = Glowny(Chat(FakeChatCompletionClient.Rzucajacy(new HttpRequestException("brak sieci"))));
		vm.NewMessageText = "pytanie";

		await vm.SendMessageCommand.ExecuteAsync(null);

		Assert.Equal(2, vm.ChatViewModel.Messages.Count);
		Assert.Contains("Nie udało się", vm.ChatViewModel.Messages[1].MessageContent);
		// W bazie została tylko wiadomość użytkownika.
		Assert.Single(await _messages.GetAllAsync());
	}

	[Fact]
	public async Task SendMessage_ResetsLoadingFlag_EvenOnFailure()
	{
		await UstawKlucz();
		var vm = Glowny(Chat(FakeChatCompletionClient.Rzucajacy(new HttpRequestException("x"))));
		vm.NewMessageText = "pytanie";

		await vm.SendMessageCommand.ExecuteAsync(null);

		Assert.False(vm.IsLoading);
	}

	[Fact]
	public async Task Initialize_LoadsExistingHistory()
	{
		await _messages.AddAsync(new Message("stara wiadomosc"));
		var vm = Glowny(Chat(new FakeChatCompletionClient()));

		await vm.InitializeAsync();

		Assert.Single(vm.ChatViewModel.Messages);
	}

	// ---------- LeftPanelViewModel ----------

	[Fact]
	public async Task Cleanup_DoesNothing_WhenDialogDeclined()
	{
		await _messages.AddAsync(new Message("zostaje"));
		var vm = LewyPanel();
		_dialogi.Zaplanuj(false);

		await vm.CleanupCommand.ExecuteAsync(null);

		Assert.Single(await _messages.GetAllAsync());
	}

	[Fact]
	public async Task Cleanup_SoftDeletesAll_AndRaisesEvent()
	{
		await _messages.AddAsync(new Message("do skasowania"));
		var vm = LewyPanel();
		bool zdarzenie = false;
		vm.OnCleanup += () => zdarzenie = true;
		_dialogi.Zaplanuj(true);

		await vm.CleanupCommand.ExecuteAsync(null);

		Assert.Empty(await _messages.GetAllAsync());
		Assert.Empty(vm.FavouriteMessages);
		Assert.True(zdarzenie);
	}

	[Fact]
	public async Task Initialize_LoadsOnlyFavourites()
	{
		await _messages.AddAsync(new Message("zwykla"));
		await _messages.AddAsync(new Message("ulubiona") { Favourite = true });
		var vm = LewyPanel();

		await vm.InitializeAsync();

		Assert.Equal("ulubiona", Assert.Single(vm.FavouriteMessages).MessageContent);
	}

	[Fact]
	public void ShowSettings_SetsVisibilityFlag()
	{
		// Komenda była wcześniej ReactiveCommand; binding w XAML zależy od tego,
		// że CommunityToolkit wygeneruje właściwość o tej samej nazwie.
		var vm = LewyPanel();

		vm.ShowSettingsCommand.Execute(null);

		Assert.True(vm.IsSettingsVisible);
	}

	[Fact]
	public async Task Export_DelegatesToService()
	{
		var vm = LewyPanel();

		await vm.ExportCommand.ExecuteAsync(null);

		Assert.Equal(1, _eksport.LiczbaEksportowWszystkich);
	}

	// ---------- RightPanelViewModel ----------

	[Fact]
	public async Task RunCode_DoesNothing_WhenConfirmationDeclined()
	{
		// Uruchomienie kodu od modelu AI wymaga potwierdzenia — to zabezpieczenie
		// nie może dać się obejść.
		var wiadomosc = await _messages.AddAsync(
			new Message("print(1)") { ProgrammingLanguage = "python" });
		var vm = PrawyPanel();
		vm.SelectMessage(wiadomosc);
		_dialogi.Zaplanuj(false);

		await vm.RunCodeCommand.ExecuteAsync(null);

		Assert.Empty(_runner.Uruchomienia);
	}

	[Fact]
	public async Task RunCode_RunsAndShowsOutput_WhenConfirmed()
	{
		var wiadomosc = await _messages.AddAsync(
			new Message("print(1)") { ProgrammingLanguage = "python" });
		var vm = PrawyPanel();
		vm.SelectMessage(wiadomosc);
		_dialogi.Zaplanuj(true);

		await vm.RunCodeCommand.ExecuteAsync(null);

		Assert.Equal(("print(1)", "python"), Assert.Single(_runner.Uruchomienia));
		Assert.Contains("wynik dzialania", vm.ConsoleOutput);
	}

	[Fact]
	public async Task RunCode_DoesNothing_WithoutProgrammingLanguage()
	{
		var wiadomosc = await _messages.AddAsync(new Message("zwykly tekst"));
		var vm = PrawyPanel();
		vm.SelectMessage(wiadomosc);

		await vm.RunCodeCommand.ExecuteAsync(null);

		Assert.Empty(_runner.Uruchomienia);
		Assert.Empty(_dialogi.ZadanePytania);
	}

	[Fact]
	public async Task SaveFavourite_PersistsEditedContent()
	{
		var wiadomosc = await _messages.AddAsync(new Message("oryginal"));
		var vm = PrawyPanel();
		vm.SelectMessage(wiadomosc);
		vm.MessageContent = "po edycji";

		await vm.SaveFavouriteCommand.ExecuteAsync(null);

		_fixture.Detach();
		var zapisana = Assert.Single(await _messages.GetAllFavouriteAsync());
		Assert.Equal("po edycji", zapisana.MessageContent);
	}

	[Fact]
	public async Task UnpinMessage_RestoresOriginalContent_AndUnsetsFavourite()
	{
		// Odpięcie z ulubionych zostawia wiadomość w rozmowie i przywraca jej
		// pierwotną treść — nazwa komendy mówi teraz dokładnie to.
		var wiadomosc = await _messages.AddAsync(new Message("oryginal") { Favourite = true });
		wiadomosc.MessageContent = "zmienione";
		await _messages.UpdateAsync(wiadomosc);

		var vm = PrawyPanel();
		vm.SelectMessage(wiadomosc);
		_dialogi.Zaplanuj(true);

		await vm.UnpinMessageCommand.ExecuteAsync(null);

		_fixture.Detach();
		var wBazie = Assert.Single(await _messages.GetAllAsync());
		Assert.Equal("oryginal", wBazie.MessageContent);
		Assert.False(wBazie.Favourite);
		Assert.Null(vm.SelectedMessage);
	}

	[Fact]
	public async Task UnpinMessage_DoesNothing_WhenDeclined()
	{
		var wiadomosc = await _messages.AddAsync(new Message("tresc") { Favourite = true });
		var vm = PrawyPanel();
		vm.SelectMessage(wiadomosc);
		_dialogi.Zaplanuj(false);

		await vm.UnpinMessageCommand.ExecuteAsync(null);

		_fixture.Detach();
		Assert.True(Assert.Single(await _messages.GetAllAsync()).Favourite);
		Assert.NotNull(vm.SelectedMessage);
	}

	[Fact]
	public void SelectMessage_ClearsPreviousConsoleOutput()
	{
		var vm = PrawyPanel();
		vm.SelectMessage(new Message("pierwsza"));
		vm.ConsoleOutput = "stare wyjscie";

		vm.SelectMessage(new Message("druga"));

		Assert.Equal(string.Empty, vm.ConsoleOutput);
		Assert.Equal("druga", vm.MessageContent);
	}

	public void Dispose() => _fixture.Dispose();
}
