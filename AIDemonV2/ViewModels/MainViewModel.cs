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

	public MainViewModel(LeftPanelViewModel leftPanelViewModel)
	{
		LeftPanelViewModel = leftPanelViewModel;
		ChatViewModel = new MainChatViewModel();
		RightPanelViewModel = new RightPanelViewModel();
		
	}

	
}
