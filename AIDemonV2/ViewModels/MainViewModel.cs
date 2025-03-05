namespace AIDemonV2.ViewModels;

public class MainViewModel : ViewModelBase
{
	public MainChatViewModel ChatViewModel { get; }

	public MainViewModel()
	{
		ChatViewModel = new MainChatViewModel();
	}
}
