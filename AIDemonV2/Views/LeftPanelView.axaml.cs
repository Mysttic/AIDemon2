using AIDemonV2.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AIDemonV2.Views;

public partial class LeftPanelView : UserControl
{
	public LeftPanelView()
	{
		InitializeComponent();
		DataContext = new LeftPanelViewModel();

	}
}