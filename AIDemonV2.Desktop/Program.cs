using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AIDemonV2.Desktop;

class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		var services = new ServiceCollection();
		ConfigureServices(services);
		var serviceProvider = services.BuildServiceProvider();

		InitializeScope(serviceProvider);

		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		// Rejestracja DbContext – connection string ustawiony wewnętrznie
		services.AddDbContext<AIDemonDbContext>(options =>
		{
			options.UseNpgsql("Host=localhost;Port=5432;Database=AIDemonDB;Username=postgres;Password=postgres;");
		});
		// Rejestracja innych serwisów...
	}

	private static void InitializeScope(ServiceProvider serviceProvider)
	{
		using (var scope = serviceProvider.CreateScope())
		{
			var dbContext = scope.ServiceProvider.GetRequiredService<AIDemonDbContext>();
			dbContext.Database.Migrate(); // lub dbContext.Database.EnsureCreated();
		}
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.UseReactiveUI();
}