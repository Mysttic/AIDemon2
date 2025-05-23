﻿using AIDemon2.Properties;
using AIDemon2.ViewModels;
using AIDemon2.Views;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AIDemon2;

internal class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static async Task Main(string[] args)
	{
		var services = new ServiceCollection();
		ConfigureServices(services);
		var serviceProvider = services.BuildServiceProvider();

		InitializeScope(serviceProvider);

		BuildAvaloniaApp(serviceProvider)
			.AfterSetup(_ => Application.Current!.Resources["Services"] = serviceProvider)
			.StartWithClassicDesktopLifetime(args);
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		// Rejestracja DbContext – connection string ustawiony wewnętrznie
		services.AddDbContext<AIDemonDbContext>(options =>
			options.UseSqlite($"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AIDemon2.db")};Password={Resources.SQLiteDBPass};"),
			ServiceLifetime.Scoped
		);

		// Rejestracja innych serwisów jako Scoped zamiast Transient
		services.AddSingleton<IMessageRepository, MessageRepository>();
		services.AddSingleton<ISettingsRepository, SettingsRepository>();
		services.AddSingleton<IChatService, ChatService>();
		services.AddSingleton<ICodeRunnerService, CodeRunnerService>();
		services.AddSingleton<IDialogService, DialogService>();
		services.AddSingleton<IMessageExportService, MessageExportService>();

		services.AddSingleton<MainViewModel>();
		services.AddSingleton<LeftPanelViewModel>();
		services.AddSingleton<MainChatViewModel>();
		services.AddSingleton<RightPanelViewModel>();

		services.AddTransient<SettingsViewModel>(); // Można zostawić Transient

		services.AddTransient<MainWindow>();
		services.AddTransient<LeftPanelView>();
		services.AddTransient<RightPanelView>();
	}

	private static void InitializeScope(ServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<AIDemonDbContext>();
		dbContext.Database.Migrate(); // lub dbContext.Database.EnsureCreated();
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.UseReactiveUI();
}