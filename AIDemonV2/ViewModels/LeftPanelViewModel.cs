using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace AIDemonV2.ViewModels;

public partial class LeftPanelViewModel : ViewModelBase
{
	public ObservableCollection<Message> SavedMessages { get; set; } = new ObservableCollection<Message>();

	private readonly ISettingsRepository _settingsRepository;

	private bool _isSettingsVisible;
	public bool IsSettingsVisible
	{
		get => _isSettingsVisible;
		set => this.RaiseAndSetIfChanged(ref _isSettingsVisible, value);
	}


	public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }

	public LeftPanelViewModel(ISettingsRepository settingsRepository)
	{
		_settingsRepository = settingsRepository;
		ShowSettingsCommand = ReactiveCommand.Create(() =>
		{
			IsSettingsVisible = true;  
		});

	}

}

