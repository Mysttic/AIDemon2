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
		LoadSettingsAsync();
	}

	[ObservableProperty]
	private string apiKey;

	[ObservableProperty]
	private string instructionPrompt;

	public List<string> AIModelsList { get; private set; } = Resources.AIModels.Split(';').ToList();

	[ObservableProperty]
	private string? aIModel;

	public List<string> ProgrammingLanguageList { get; } = ProgrammingLanguageConfig.Languages.Keys.ToList();

	[ObservableProperty]
	private string? programmingLanguage;

	private async void LoadSettingsAsync()
	{
		var settings = await _settingsRepository.Get();
		if (settings != null)
		{
			ApiKey = settings.ApiKey;
			InstructionPrompt = settings.InstructionPrompt;
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
			if (settings.InstructionPrompt != InstructionPrompt ||
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