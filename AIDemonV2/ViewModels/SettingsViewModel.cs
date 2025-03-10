using AIDemonV2.Properties;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AIDemonV2.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly IServiceProvider _serviceProvider;

	public event Action? CloseRequested;

	public SettingsViewModel(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		LoadSettings();
	}

	[ObservableProperty]
	private string apiKey;

	[ObservableProperty]
	private string instructionPrompt;

	public List<string> AIModelsList { get; private set; } = Resources.AIModels.Split(';').ToList();
	[ObservableProperty]
	private string? aIModel;

	public List<string> ProgrammingLanguageList { get; } = Resources.ProgrammingLanguages.Split(';').ToList();
	[ObservableProperty]
	private string? programmingLanguage;

	private async void LoadSettings()
	{
		using var scope = _serviceProvider.CreateScope();
		var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

		var settings = await settingsRepository.Get();
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
		using var scope = _serviceProvider.CreateScope();
		var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

		var settings = await settingsRepository.Get();
		if (settings != null)
		{
			settings.ApiKey = ApiKey;
			settings.InstructionPrompt = InstructionPrompt;
			settings.AIModel = AIModel;
			settings.ProgrammingLanguage = ProgrammingLanguage;

			await settingsRepository.UpdateAsync(settings);
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
