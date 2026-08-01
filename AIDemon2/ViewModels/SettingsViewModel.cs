using System.Collections.ObjectModel;
using AIDemon2.Services.ModelCatalog;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIDemon2.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly ISettingsRepository _settingsRepository;
	private readonly IChatService _chatService;
	private readonly IModelCatalog _modelCatalog;

	public event Action? CloseRequested;

	public SettingsViewModel(ISettingsRepository settingsRepository, IChatService chatService,
		IModelCatalog modelCatalog)
	{
		_settingsRepository = settingsRepository;
		_chatService = chatService;
		_modelCatalog = modelCatalog;
	}

	[ObservableProperty]
	private string apiKey = string.Empty;

	[ObservableProperty]
	private string instructionPrompt = string.Empty;

	/// <summary>
	/// Lista modeli pobierana z OpenRoutera. Kolekcja obserwowalna, bo wypełnia się
	/// asynchronicznie — okno otwiera się od razu, lista dochodzi po chwili.
	/// </summary>
	public ObservableCollection<string> AIModelsList { get; } = new();

	[ObservableProperty]
	private string? aIModel;

	public List<string> ProgrammingLanguageList { get; } = ProgrammingLanguageConfig.Languages.Keys.ToList();

	[ObservableProperty]
	private string? programmingLanguage;

	/// <summary>
	/// Było "async void" wołane z konstruktora: wyjątek z tej metody nie miał gdzie
	/// wypłynąć i ubijał proces bez śladu.
	/// </summary>
	public async Task InitializeAsync()
	{
		var settings = await _settingsRepository.Get();
		string? zapisanyModel = settings?.AIModel;

		// Zapisany model trafia na listę PRZED przypisaniem zaznaczenia, bo ComboBox
		// z pustym ItemsSource odrzuca SelectedItem i nie przywraca go później.
		// Dotyczy to również modelu wycofanego przez dostawcę — bez tego pole byłoby
		// puste, a zapis ustawień po cichu skasowałby wybór użytkownika.
		if (!string.IsNullOrWhiteSpace(zapisanyModel))
			AIModelsList.Add(zapisanyModel);

		// Pola formularza wypełniamy z bazy ZANIM pójdziemy po listę modeli do sieci.
		// Odwrotna kolejność otwierała okno z pustym kluczem API i pustym promptem na
		// czas trwania zapytania — a kliknięcie "Save" w tym oknie zapisywało te pustki
		// do bazy, kasując użytkownikowi klucz.
		if (settings != null)
		{
			ApiKey = settings.ApiKey ?? string.Empty;
			InstructionPrompt = settings.InstructionPrompt ?? string.Empty;
			AIModel = zapisanyModel;
			ProgrammingLanguage = settings.ProgrammingLanguage;
		}

		foreach (var model in await _modelCatalog.GetModelsAsync())
			if (!AIModelsList.Contains(model))
				AIModelsList.Add(model);
	}

	[RelayCommand]
	private async Task Save()
	{
		var settings = await _settingsRepository.Get();
		if (settings != null)
		{
			// Klucz API MUSI być na tej liście: bez tego zapisanie nowego klucza
			// nie odtwarzało klienta i aplikacja do restartu używała starego.
			if (settings.ApiKey != ApiKey ||
				settings.InstructionPrompt != InstructionPrompt ||
				settings.AIModel != AIModel ||
				settings.ProgrammingLanguage != ProgrammingLanguage)
				_chatService.ResetClient();

			settings.ApiKey = ApiKey;
			settings.InstructionPrompt = InstructionPrompt;
			settings.AIModel = AIModel;
			settings.ProgrammingLanguage = ProgrammingLanguage;

			await _settingsRepository.UpdateAsync(settings);
		}
		CloseSettings();
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseSettings();
	}

	private void CloseSettings()
	{
		CloseRequested?.Invoke();
		CloseRequested = null;
	}
}