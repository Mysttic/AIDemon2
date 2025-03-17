using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace AIDemon2.ViewModels;

public partial class LeftPanelViewModel : ObservableObject
{
	public ObservableCollection<Message> FavouriteMessages { get; set; } = new ObservableCollection<Message>();

	private readonly IMessageRepository _messageRepository;
	private readonly IDialogService _dialogService;
	private readonly IMessageExportService _messageExportService;

	private bool _isSettingsVisible;

	public bool IsSettingsVisible
	{
		get => _isSettingsVisible;
		set => SetProperty(ref _isSettingsVisible, value);
	}

	public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }

	public event Action OnCleanup;

	public LeftPanelViewModel(
		IMessageRepository messageRepository,
		IDialogService dialogService,
		IMessageExportService messageExportService)
	{
		_messageRepository = messageRepository;
		_dialogService = dialogService;
		_messageExportService = messageExportService;
		ShowSettingsCommand = ReactiveCommand.Create(() =>
		{
			IsSettingsVisible = true;
		});
		_ = LoadFavouriteMessages();
	}

	public async Task LoadFavouriteMessages()
	{
		FavouriteMessages.Clear();
		await _messageRepository.GetAllFavouriteAsync().ContinueWith(task =>
		{
			FavouriteMessages.AddRange(task.Result);
		});
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