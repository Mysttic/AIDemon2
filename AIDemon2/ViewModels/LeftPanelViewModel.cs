using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AIDemon2.ViewModels;

public partial class LeftPanelViewModel : ObservableObject
{
	// { get; } zamiast { get; set; }: kolekcja jest tylko mutowana, nigdy podmieniana,
	// a o zmianach zawartości informuje sama ObservableCollection.
	public ObservableCollection<Message> FavouriteMessages { get; } = new();

	private readonly IMessageRepository _messageRepository;
	private readonly IDialogService _dialogService;
	private readonly IMessageExportService _messageExportService;

	private bool _isSettingsVisible;

	public bool IsSettingsVisible
	{
		get => _isSettingsVisible;
		set => SetProperty(ref _isSettingsVisible, value);
	}

	public event Action? OnCleanup;

	public LeftPanelViewModel(
		IMessageRepository messageRepository,
		IDialogService dialogService,
		IMessageExportService messageExportService)
	{
		_messageRepository = messageRepository;
		_dialogService = dialogService;
		_messageExportService = messageExportService;
	}

	/// <summary>
	/// Było ReactiveCommand — jedyny użytek z ReactiveUI w całym projekcie.
	/// CommunityToolkit generuje z tej metody właściwość ShowSettingsCommand,
	/// więc binding w LeftPanelView.axaml pozostaje bez zmian.
	/// </summary>
	[RelayCommand]
	private void ShowSettings()
	{
		IsSettingsVisible = true;
	}

	/// <summary>Wczytanie danych wyniesione z konstruktora — patrz MainChatViewModel.</summary>
	public Task InitializeAsync() => LoadFavouriteMessages();

	public async Task LoadFavouriteMessages()
	{
		// ContinueWith bez opcji planisty gubił kontekst i połykał wyjątki
		// (task.Result rzuca AggregateException wewnątrz kontynuacji).
		var favourites = await _messageRepository.GetAllFavouriteAsync();
		FavouriteMessages.Clear();
		// Pętla zamiast AddRange: to była metoda rozszerzająca z DynamicData,
		// pakietu wciąganego wyłącznie przez ReactiveUI.
		foreach (var favourite in favourites)
			FavouriteMessages.Add(favourite);
	}

	[RelayCommand]
	private async Task Cleanup()
	{
		bool confirmed = await _dialogService.ShowConfirmationDialog("Confirmation", "Are you sure you want to delete all messages?");
		if (confirmed)
		{
			FavouriteMessages.Clear();
			await _messageRepository.DeleteAllAsync();
			await LoadFavouriteMessages();
			OnCleanup?.Invoke();
		}
	}

	[RelayCommand]
	private async Task Export()
	{
		await _messageExportService.ExportMessagesAsync();
		await _dialogService.ShowConfirmationDialog("Export", "Messages exported successfully.", true);
	}
}