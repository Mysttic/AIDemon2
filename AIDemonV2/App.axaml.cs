using AIDemonV2.ViewModels;
using AIDemonV2.Views;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged;

namespace AIDemonV2;

[DoNotNotify]
public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var serviceProvider = (IServiceProvider)this.Resources["Services"];
			var dialogService = serviceProvider.GetRequiredService<IDialogService>();

			var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
			dialogService.Initialize(mainWindow);

			desktop.MainWindow = mainWindow;
			desktop.MainWindow.DataContext = serviceProvider.GetRequiredService<MainViewModel>();
		}

		base.OnFrameworkInitializationCompleted();
	}
}