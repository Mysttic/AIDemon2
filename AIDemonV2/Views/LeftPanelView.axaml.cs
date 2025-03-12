using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged;

namespace AIDemonV2.Views;

[DoNotNotify]
public partial class LeftPanelView : UserControl
{
	public LeftPanelView()
	{
		InitializeComponent();
		DataContext = ((IServiceProvider)Application.Current!.Resources["Services"])
			.GetRequiredService<LeftPanelViewModel>();
		SettingsButton.Click += OnSettingsButtonClick;
		this.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
	}

	private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
	{
		var mainView = this.FindAncestorOfType<MainView>();
		if (mainView is not null)
		{
			bool isSettingsVisible = mainView.SettingsViewControl.IsVisible;
			if (!isSettingsVisible)
				mainView.SettingsViewControl.IsVisible = true;
		}
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