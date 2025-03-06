using AIDemonV2.ViewModels;
using Avalonia.Controls;

namespace AIDemonV2.Views;

public partial class MainChatView : UserControl
{
	public MainChatView()
	{
		InitializeComponent();
		DataContext = new MainChatViewModel();
	}

}