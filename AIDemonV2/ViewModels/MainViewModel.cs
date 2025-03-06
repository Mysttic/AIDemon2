using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace AIDemonV2.ViewModels;

public class MainViewModel : ViewModelBase
{
	public LeftPanelViewModel LeftPanelViewModel { get; }
	public MainChatViewModel ChatViewModel { get; }
	public RightPanelViewModel RightPanelViewModel { get; }

	ISettingsRepository _settingsRepository;

	public MainViewModel(ISettingsRepository settingsRepository)
	{
		_settingsRepository = settingsRepository;
		LeftPanelViewModel = new LeftPanelViewModel(settingsRepository);
		ChatViewModel = new MainChatViewModel();
		RightPanelViewModel = new RightPanelViewModel();
		
	}

	
}
