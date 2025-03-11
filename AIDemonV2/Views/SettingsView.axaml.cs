using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged;

namespace AIDemonV2.Views;

[DoNotNotify]
public partial class SettingsView : UserControl
{
	public SettingsView()
	{
		InitializeComponent();
		DataContext = ((IServiceProvider)Application.Current!.Resources["Services"])
			.GetRequiredService<SettingsViewModel>();
		SaveButton.Click += Close;
		CancelButton.Click += Close;
	}

	private void Close(object? sender, RoutedEventArgs e)
	{
		var mainView = this.FindAncestorOfType<MainView>();
		if (mainView is not null)
		{
			bool isSettingsVisible = mainView.SettingsViewControl.IsVisible;
			if (isSettingsVisible)
				mainView.SettingsViewControl.IsVisible = false;
		}
	}
}