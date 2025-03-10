using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace AIDemonV2.Views;

public partial class RightPanelView : UserControl
{
    public RightPanelView()
    {
        InitializeComponent();
        DataContext = ((IServiceProvider)Application.Current!.Resources["Services"])
			.GetRequiredService<RightPanelViewModel>();
	}
}