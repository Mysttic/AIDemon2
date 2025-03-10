using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace AIDemonV2.ViewModels;

public partial class LeftPanelViewModel : ViewModelBase
{
	public ObservableCollection<Message> FavouriteMessages { get; set; } = new ObservableCollection<Message>();

	private readonly IMessageRepository _messageRepository;

	private bool _isSettingsVisible;
	public bool IsSettingsVisible
	{
		get => _isSettingsVisible;
		set => this.RaiseAndSetIfChanged(ref _isSettingsVisible, value);
	}


	public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }

	public LeftPanelViewModel(IMessageRepository messageRepository)
	{
		_messageRepository = messageRepository;
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
}

