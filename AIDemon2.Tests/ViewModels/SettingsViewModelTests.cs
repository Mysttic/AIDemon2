using AIDemon2.Services.ModelCatalog;
using AIDemon2.Tests.Infrastructure;
using AIDemon2.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIDemon2.Tests.ViewModels;

/// <summary>Lista modeli w ustawieniach — pochodzi z katalogu OpenRoutera.</summary>
public class SettingsViewModelTests : IDisposable
{
	private readonly SqliteDbFixture _fixture = new();
	private readonly SettingsRepository _settings;

	public SettingsViewModelTests()
	{
		_settings = new SettingsRepository(_fixture);
	}

	private sealed class FakeKatalog : IModelCatalog
	{
		private readonly string[] _modele;
		public FakeKatalog(params string[] modele) => _modele = modele;
		public Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken ct = default) =>
			Task.FromResult<IReadOnlyList<string>>(_modele);
	}

	private async Task<SettingsViewModel> Utworz(string? zapisanyModel, params string[] katalog)
	{
		var s = await _settings.Get();
		s!.AIModel = zapisanyModel;
		s.ApiKey = "klucz";
		await _settings.UpdateAsync(s);

		var chat = new ChatService(new MessageRepository(_fixture), _settings,
			_ => new FakeChatCompletionClient(), NullLogger<ChatService>.Instance);

		var vm = new SettingsViewModel(_settings, chat, new FakeKatalog(katalog));
		await vm.InitializeAsync();
		return vm;
	}

	[Fact]
	public async Task Wypelnia_Liste_Z_Katalogu()
	{
		var vm = await Utworz(null, "openai/gpt-4o", "anthropic/claude-sonnet-4.5");

		Assert.Equal(2, vm.AIModelsList.Count);
	}

	[Fact]
	public async Task Zaznacza_Model_Zapisany_W_Bazie()
	{
		// Regresja: zaznaczenie ustawiane przed wypełnieniem listy było odrzucane
		// przez ComboBox, więc pole zostawało puste mimo modelu w bazie.
		var vm = await Utworz("openai/gpt-4o", "openai/gpt-4o", "anthropic/claude-sonnet-4.5");

		Assert.Equal("openai/gpt-4o", vm.AIModel);
		Assert.Contains("openai/gpt-4o", vm.AIModelsList);
	}

	[Fact]
	public async Task Dodaje_Model_Wycofany_Przez_Dostawce()
	{
		// Bez tego pole byłoby puste, a zapis ustawień skasowałby wybór użytkownika.
		var vm = await Utworz("stary/model-wycofany", "openai/gpt-4o");

		Assert.Equal("stary/model-wycofany", vm.AIModel);
		Assert.Equal("stary/model-wycofany", vm.AIModelsList[0]);
	}

	public void Dispose() => _fixture.Dispose();
}
