using AIDemon2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;

namespace AIDemon2.Views;

public partial class SettingsView : UserControl
{
	public SettingsView()
	{
		InitializeComponent();
		var viewModel = ViewServices.Get<SettingsViewModel>();
		DataContext = viewModel;
		// Wczytanie ustawień przeniesione z konstruktora ViewModelu.
		Loaded += async (_, _) => await viewModel.InitializeAsync();
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