using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AIDemonV2.Views;

public partial class RightPanelView : UserControl
{
    public RightPanelView()
    {
        InitializeComponent();
        DataContext = new RightPanelViewModel();
    }
}