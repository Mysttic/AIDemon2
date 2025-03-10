using AIDemonV2.Properties;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AIDemonV2.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly IServiceProvider _serviceProvider;

	public event Action? CloseRequested;

	public SettingsViewModel(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		LoadSettings();
		LoadAiModels();
	}

	[ObservableProperty]
	private string apiKey;

	[ObservableProperty]
	private string instructionPrompt;

	public List<AIModel> AIModelsList { get; private set; } = new();
	[ObservableProperty]
	private AIModel? selectedAIModel;

	public List<string> ProgrammingLanguageList { get; } =  Resources.ProgrammingLanguages.Split(';').ToList();
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
			SelectedAIModel = settings.SelectedAIModel;
			ProgrammingLanguage = settings.ProgrammingLanguage;
		}
	}

	private async void LoadAiModels()
	{
		using var scope = _serviceProvider.CreateScope();
		var aiModelRepository = scope.ServiceProvider.GetRequiredService<IAIModelRepository>();

		var aimodels = await aiModelRepository.GetAllAsync();
		AIModelsList = aimodels.ToList();
		OnPropertyChanged(nameof(AIModelsList));
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
			settings.SelectedAIModel = SelectedAIModel;
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
