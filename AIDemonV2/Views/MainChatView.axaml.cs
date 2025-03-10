using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AIDemonV2.Views;

public partial class MainChatView : UserControl
{
	public MainChatView()
	{
		InitializeComponent();
		var services = (IServiceProvider)Application.Current!.Resources["Services"];
		DataContext = services.GetRequiredService<MainChatViewModel>();
		if (DataContext is MainChatViewModel vm)
			vm.ScrollRequested += ScrollToBottom;
	}

	private void ScrollToBottom()
	{
		ChatScrollViewer.ScrollToEnd();
	}

}