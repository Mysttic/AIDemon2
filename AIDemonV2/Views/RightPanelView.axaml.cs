using AIDemonV2.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged;

namespace AIDemonV2.Views;

[DoNotNotify]
public partial class RightPanelView : UserControl
{
	public RightPanelView()
	{
		InitializeComponent();
		DataContext = ((IServiceProvider)Application.Current!.Resources["Services"])
			.GetRequiredService<RightPanelViewModel>();
	}
}