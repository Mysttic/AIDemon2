using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace AIDemonV2.Views;

public partial class MainView : UserControl
{
	private bool _isLeftPanelVisible = true;
	private bool _isRightPanelVisible = true;

	private readonly LeftPanelViewModel _leftPanelViewModel;
	private readonly IServiceProvider _services;

	public MainView()
	{
		InitializeComponent();
		ToggleLeftPanelButton.Click += OnToggleLeftPanelClick;
		ToggleRightPanelButton.Click += OnToggleRightPanelClick;

		_services = (IServiceProvider)Application.Current!.Resources["Services"];
		var vm = _services.GetRequiredService<MainViewModel>();
		DataContext = vm;

		_leftPanelViewModel = vm.LeftPanelViewModel;

		// Obsługa otwierania okna SettingsView
		_leftPanelViewModel.ShowSettingsCommand.Subscribe(_ =>
		{
			OpenSettingsView();
		});

	}

	private void OpenSettingsView()
	{
		var settingsVM = new SettingsViewModel(_services.GetRequiredService<ISettingsRepository>());

		settingsVM.CloseRequested += () =>
		{
			Dispatcher.UIThread.Post(() =>
			{
				_leftPanelViewModel.IsSettingsVisible = false;
				SettingsViewControl.IsVisible = false;
				SettingsViewControl.DataContext = null; // Reset DataContext
			});
		};

		Dispatcher.UIThread.Post(() =>
		{
			SettingsViewControl.DataContext = settingsVM;
			_leftPanelViewModel.IsSettingsVisible = true;
			SettingsViewControl.IsVisible = true;
		});
	}

	private void OnToggleLeftPanelClick(object? sender, RoutedEventArgs e)
	{
		var row = LeftPanelGrid.RowDefinitions[1];

		if (_isLeftPanelVisible)
		{
			row.Height = new GridLength(0);  // Ukrywamy zawartość panelu
			LeftPanel.IsVisible = false;
			ToggleLeftPanelButton.Content = ">>";
			_isLeftPanelVisible = false;
		}
		else
		{
			row.Height = new GridLength(1, GridUnitType.Star);
			LeftPanel.IsVisible = true;
			ToggleLeftPanelButton.Content = "<<";
			_isLeftPanelVisible = true;
		}
	}

	private void OnToggleRightPanelClick(object? sender, RoutedEventArgs e)
	{
		var row = RightPanelGrid.RowDefinitions[1];

		if (_isRightPanelVisible)
		{
			row.Height = new GridLength(0);
			RightPanel.IsVisible = false;
			ToggleRightPanelButton.Content = "<<";
			_isRightPanelVisible = false;
		}
		else
		{
			row.Height = new GridLength(1, GridUnitType.Star); // Powrót do domyślnego rozmiaru
			RightPanel.IsVisible = true;
			ToggleRightPanelButton.Content = ">>";
			_isRightPanelVisible = true;
		}
	}
}