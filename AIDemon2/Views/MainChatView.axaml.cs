using AIDemon2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged;

namespace AIDemon2.Views;

[DoNotNotify]
public partial class MainChatView : UserControl
{
	public MainChatView()
	{
		InitializeComponent();
		var services = (IServiceProvider)Application.Current!.Resources["Services"];
		DataContext = services.GetRequiredService<MainChatViewModel>();
		if (DataContext is MainChatViewModel vm)
			vm.ScrollRequested += ScrollToBottom;
		this.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
	}

	private void ScrollToBottom()
	{
		ChatScrollViewer.ScrollToEnd();
	}

	private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (e.ClickCount == 2) // Sprawdza, czy to double click
		{
			var control = e.Source as Control;
			if (control?.DataContext is Message message)
			{
				var mainView = this.FindAncestorOfType<MainView>();
				if (mainView is not null)
				{
					if (mainView.DataContext is MainViewModel vm)
					{
						vm.RightPanelViewModel.SelectMessage(message);
						if (!mainView.RightPanel.IsVisible)
							mainView.ToggleRightPanelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
					}
				}
			}
		}
	}
}