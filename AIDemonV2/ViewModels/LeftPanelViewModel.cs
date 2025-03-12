using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace AIDemonV2.ViewModels;

public partial class LeftPanelViewModel : ObservableObject
{
	public ObservableCollection<Message> FavouriteMessages { get; set; } = new ObservableCollection<Message>();

	private readonly IMessageRepository _messageRepository;
	private readonly IDialogService _dialogService;

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
		IDialogService dialogService)
	{
		_messageRepository = messageRepository;
		_dialogService = dialogService;
		ShowSettingsCommand = ReactiveCommand.Create(() =>
		{
			IsSettingsVisible = true;
		});
		_ = LoadFavouriteMessages();
	}

	public async Task LoadFavouriteMessages()
	{
		FavouriteMessages.Clear();
		await _messageRepository.GetAllAsync().ContinueWith(task =>
		{
			foreach (var message in task.Result)
			{
				if (message.Favourite)
				{
					FavouriteMessages.Add(message);
				}
			}
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
}