using AIDemon2.Properties;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIDemon2.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly ISettingsRepository _settingsRepository;
	private readonly IChatService _chatService;

	public event Action? CloseRequested;

	public SettingsViewModel(ISettingsRepository settingsRepository, IChatService chatService)
	{
		_settingsRepository = settingsRepository;
		_chatService = chatService;
	}

	[ObservableProperty]
	private string apiKey = string.Empty;

	[ObservableProperty]
	private string instructionPrompt = string.Empty;

	// Lista jest budowana raz i nigdy nie podmieniana — powiadomienie zbędne.
	public List<string> AIModelsList { get; } = Resources.AIModels.Split(';').ToList();

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
		if (settings != null)
		{
			ApiKey = settings.ApiKey ?? string.Empty;
			InstructionPrompt = settings.InstructionPrompt ?? string.Empty;
			AIModel = settings.AIModel;
			ProgrammingLanguage = settings.ProgrammingLanguage;
		}
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