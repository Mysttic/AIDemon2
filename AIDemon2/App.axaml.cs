using AIDemon2.ViewModels;
using AIDemon2.Views;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIDemon2;

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
			if (Resources["Services"] is not IServiceProvider serviceProvider)
				throw new InvalidOperationException("The dependency container was not placed in the application resources.");

			var dialogService = serviceProvider.GetRequiredService<IDialogService>();

			var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
			dialogService.Initialize(mainWindow);

			var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();
			desktop.MainWindow = mainWindow;
			desktop.MainWindow.DataContext = mainViewModel;

			// Wczytanie danych po otwarciu okna, a nie w konstruktorach ViewModeli.
			// async void jest tu poprawne (handler zdarzenia UI), a wyjątek jest
			// łapany i logowany zamiast ubijać proces.
			mainWindow.Opened += async (_, _) =>
			{
				var logger = serviceProvider.GetRequiredService<ILogger<App>>();
				try
				{
					await mainViewModel.InitializeAsync();
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Loading the initial data failed");
				}
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}