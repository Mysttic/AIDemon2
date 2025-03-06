namespace AIDemonV2.ViewModels;

public class MainViewModel : ViewModelBase
{
	public LeftPanelViewModel LeftPanelViewModel { get; }
	public MainChatViewModel ChatViewModel { get; }

	public MainViewModel()
	{
		LeftPanelViewModel = new LeftPanelViewModel();
		ChatViewModel = new MainChatViewModel();
	}
}
