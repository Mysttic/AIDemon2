using AIDemon2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;

namespace AIDemon2.Views;

public partial class RightPanelView : UserControl
{
	public RightPanelView()
	{
		InitializeComponent();
		DataContext = ViewServices.Get<RightPanelViewModel>();
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