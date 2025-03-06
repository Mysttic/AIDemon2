using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AIDemonV2.Views;

public partial class LeftPanelView : UserControl
{
	public LeftPanelView()
	{
		InitializeComponent();
		DataContext = ((IServiceProvider)Application.Current!.Resources["Services"])
			.GetRequiredService<LeftPanelViewModel>();
		SettingsButton.Click += OnSettingsButtonClick;
	}

	private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
	{
		var mainView = this.FindAncestorOfType<MainView>();
		if (mainView is not null)
		{
			bool isSettingsVisible = mainView.SettingsViewControl.IsVisible;
			mainView.SettingsViewControl.IsVisible = !isSettingsVisible;
		}
	}
}