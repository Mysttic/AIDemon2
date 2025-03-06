using ReactiveUI;
using System.Threading.Tasks;

namespace AIDemonV2.ViewModels;

public class MainViewModel : ViewModelBase
{
	public LeftPanelViewModel LeftPanelViewModel { get; }
	public MainChatViewModel ChatViewModel { get; }
	public RightPanelViewModel RightPanelViewModel { get; }

	public MainViewModel()
	{
		LeftPanelViewModel = new LeftPanelViewModel();
		ChatViewModel = new MainChatViewModel();
		RightPanelViewModel = new RightPanelViewModel();
	}

	private bool _isNotBusy = true;
	public bool IsNotBusy
	{
		get => _isNotBusy;
		set => this.RaiseAndSetIfChanged(ref _isNotBusy, value);
	}

	private string _sendButtonIcon = "M0,0 L10,5 L0,10 Z"; // Ikona wysyłania (domyślna)
	public string SendButtonIcon
	{
		get => _sendButtonIcon;
		set => this.RaiseAndSetIfChanged(ref _sendButtonIcon, value);
	}

	// metoda przykładowa zmieniająca ikonę i stan
	private async Task SendMessageAsync()
	{
		IsNotBusy = false;
		SendButtonIcon = "M5,0 L5,10 M0,5 L10,5"; // np. ikona oczekiwania

		// tutaj logika wysyłania wiadomości
		await Task.Delay(1000);

		SendButtonIcon = "M0,0 L10,5 L0,10 Z"; // przywrócenie ikony wysyłania
		IsNotBusy = true;
	}
}
