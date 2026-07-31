using AIDemon2.Domain;
using AIDemon2.ViewModels;
using AIDemon2.Views;
using Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AIDemon2.Services.Logging;
using Microsoft.Extensions.Logging;

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
		var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
		{
			// Walidacja przy starcie zamiast wyjątku przy pierwszym użyciu:
			// ValidateScopes wyłapie ponowne wstrzyknięcie usługi Scoped
			// do Singletona, ValidateOnBuild — brakującą rejestrację.
			ValidateScopes = true,
			ValidateOnBuild = true
		});

		RegisterGlobalExceptionHandlers(serviceProvider);

		InitializeScope(serviceProvider);

		BuildAvaloniaApp(serviceProvider)
			.AfterSetup(_ => Application.Current!.Resources["Services"] = serviceProvider)
			.StartWithClassicDesktopLifetime(args);
	}

	/// <summary>
	/// Ostatnia linia obrony. Bez tego wyjątek spoza obsłużonej ścieżki ubijał proces
	/// bez żadnego śladu — ani w oknie, ani w pliku — więc zgłoszenie użytkownika
	/// „aplikacja się zamyka" nie dawało się zdiagnozować.
	/// </summary>
	private static void RegisterGlobalExceptionHandlers(IServiceProvider serviceProvider)
	{
		var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
			logger.LogCritical(e.ExceptionObject as Exception,
				"Nieobsłużony wyjątek; zamykanie aplikacji: {Terminating}", e.IsTerminating);

		// Wyjątek z zadania, na które nikt nie czekał (typowo "_ = SomethingAsync()").
		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			logger.LogError(e.Exception, "Nieobserwowany wyjątek zadania w tle");
			e.SetObserved();
		};
	}

	/// <summary>Internal, żeby test mógł zweryfikować, że kontener DI w ogóle się składa —
	/// błąd w rejestracjach objawia się dopiero jako crash przy starcie aplikacji.</summary>
	internal static void ConfigureServices(IServiceCollection services)
	{
		// Rejestracja DbContext – ścieżka i klucz z DatabaseLocation, wspólne
		// z fabryką design-time, żeby narzędzia EF nie rozjechały się z aplikacją.
		// Fabryka, nie AddDbContext: repozytoria są Singletonami i wstrzyknięty
		// kontekst Scoped stałby się captive dependency żyjącą przez cały proces.
		services.AddDbContextFactory<AIDemonDbContext>(options =>
			options.UseSqlite(DatabaseLocation.ConnectionString));

		services.AddLogging(builder => builder
			.SetMinimumLevel(LogLevel.Information)
			.AddProvider(new FileLoggerProvider()));

		// Fabryka klienta AI zamiast "new" w środku ChatService — dzięki temu
		// serwis da się przetestować bez wychodzenia w sieć.
		services.AddSingleton<Func<string, IChatCompletionClient>>(
			_ => apiKey => new IoIntelligenceChatClient(apiKey));

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
		var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

		try
		{
			var contextFactory = serviceProvider
				.GetRequiredService<IDbContextFactory<AIDemonDbContext>>();
			using var dbContext = contextFactory.CreateDbContext();
			dbContext.Database.Migrate();

			logger.LogInformation("Baza gotowa: {Sciezka}", DatabaseLocation.DatabasePath);
		}
		catch (Exception ex)
		{
			// Ten kod wykonuje się PRZED pokazaniem okna. Bez tego bloku każdy problem
			// z bazą — brak uprawnień do katalogu, uszkodzony plik, zły klucz — kończył
			// się zamknięciem procesu bez jakiegokolwiek komunikatu i bez śladu w logu.
			logger.LogCritical(ex, "Nie udało się przygotować bazy danych ({Sciezka})",
				DatabaseLocation.DatabasePath);

			throw new InvalidOperationException(
				$"Nie udało się otworzyć bazy danych w {DatabaseLocation.DatabasePath}. " +
				$"Szczegóły zapisano w {FileLoggerProvider.DefaultDirectory}.", ex);
		}
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
}