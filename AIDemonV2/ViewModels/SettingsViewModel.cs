﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace AIDemonV2.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly ISettingsRepository _settingsRepository;

	public SettingsViewModel(ISettingsRepository settingsRepository)
	{
		_settingsRepository = settingsRepository;
		LoadSettings();
	}

	[ObservableProperty]
	private string apiKey;

	[ObservableProperty]
	private string instructionPrompt;

	[ObservableProperty]
	private AIModel? selectedAIModel;

	[ObservableProperty]
	private string? programmingLanguage;

	public event Action CloseRequested;

	private async void LoadSettings()
	{
		var settings = await _settingsRepository.Get();
		if (settings != null)
		{
			ApiKey = settings.ApiKey;
			InstructionPrompt = settings.InstructionPrompt;
			SelectedAIModel = settings.SelectedAIModel;
			ProgrammingLanguage = settings.ProgrammingLanguage;
		}
	}

	[RelayCommand]
	private async Task Save()
	{
		var settings = await _settingsRepository.Get();
		if (settings != null)
		{
			settings.ApiKey = ApiKey;
			settings.InstructionPrompt = InstructionPrompt;
			settings.SelectedAIModel = SelectedAIModel;
			settings.ProgrammingLanguage = ProgrammingLanguage;

			await _settingsRepository.Update(settings);
		}
		CloseRequested?.Invoke();
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseRequested?.Invoke();
	}
}
