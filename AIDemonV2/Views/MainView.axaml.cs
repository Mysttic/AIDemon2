using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AIDemonV2.Views;

public partial class MainView : UserControl
{
	private bool _isLeftPanelVisible = true;
	private bool _isRightPanelVisible = true;

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
			//SettingsButton.IsVisible = false;
			//LeftPanelList.IsVisible = false;
			ToggleLeftPanelButton.Content = ">>";
			_isLeftPanelVisible = false;
		}
		else
		{
			row.Height = new GridLength(1, GridUnitType.Star);
			LeftPanel.IsVisible = true;
			//SettingsButton.IsVisible = true;
			//LeftPanelList.IsVisible = true;
			ToggleLeftPanelButton.Content = "<<";
			_isLeftPanelVisible = true;
		}
	}

	private void OnToggleRightPanelClick(object? sender, RoutedEventArgs e)
	{
		var col = RightPanelGrid.ColumnDefinitions[0];

		if (_isRightPanelVisible)
		{
			col.Width = new GridLength(0);
			RightPanelEditor.IsVisible = false;
			RightPanelButtons.IsVisible = false; // Ukrywa przyciski Save/Action/Remove
			ToggleRightPanelButton.Content = "<<";
			_isRightPanelVisible = false;
		}
		else
		{
			col.Width = new GridLength(1, GridUnitType.Star); // Powrót do domyślnego rozmiaru
			RightPanelEditor.IsVisible = true;
			RightPanelButtons.IsVisible = true; // Pokazuje przyciski
			ToggleRightPanelButton.Content = ">>";
			_isRightPanelVisible = true;
		}
	}
}