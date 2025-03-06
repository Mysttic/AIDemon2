using AIDemonV2.ViewModels;
using AIDemonV2.Views;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AIDemonV2;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		var provider = (IServiceProvider)this.Resources["ServiceProvider"];

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = provider.GetRequiredService<MainWindow>();
			desktop.MainWindow.DataContext = provider.GetRequiredService<MainViewModel>();
		}

		base.OnFrameworkInitializationCompleted();
	}
}
