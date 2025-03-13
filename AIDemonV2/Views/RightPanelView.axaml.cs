using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged;

namespace AIDemonV2.Views;

[DoNotNotify]
public partial class RightPanelView : UserControl
{
	public RightPanelView()
	{
		InitializeComponent();
		DataContext = ((IServiceProvider)Application.Current!.Resources["Services"])
			.GetRequiredService<RightPanelViewModel>();
		if (DataContext is RightPanelViewModel vm)
			vm.MessageUpdated += OnRightPanelMessageUpdated;
	}

	private void OnRightPanelMessageUpdated(Message message)
	{
		if (message != null && message.Favourite)
		{
			var mainView = this.FindAncestorOfType<MainView>();
			if (mainView is not null)
			{
				if (mainView.DataContext is MainViewModel vm)
				{
					if (!mainView.LeftPanel.IsVisible)
						mainView.ToggleLeftPanelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
				}
			}
		}
	}
}