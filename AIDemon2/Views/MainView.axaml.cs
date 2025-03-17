using Avalonia.Controls;
using Avalonia.Interactivity;
using PropertyChanged;

namespace AIDemon2.Views;

[DoNotNotify]
public partial class MainView : UserControl
{
	private bool _isLeftPanelVisible = false;
	private bool _isRightPanelVisible = false;

	public MainView()
	{
		InitializeComponent();
		ToggleLeftPanelButton.Click += OnToggleLeftPanelClick;
		ToggleRightPanelButton.Click += OnToggleRightPanelClick;
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