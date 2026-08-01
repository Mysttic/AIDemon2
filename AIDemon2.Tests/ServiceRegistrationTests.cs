using AIDemon2;
using AIDemon2.Services.ChatService;
using AIDemon2.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AIDemon2.Tests;

/// <summary>
/// Błąd w rejestracjach kontenera nie jest łapany przez kompilator — objawia się
/// dopiero jako wyjątek przy starcie aplikacji, u użytkownika. Ten test sprawdza
/// sam graf zależności, bez tworzenia obiektów, więc nie dotyka bazy ani sieci.
/// </summary>
public class ServiceRegistrationTests
{
	private static ServiceProvider Zbuduj() =>
		BuildCollection().BuildServiceProvider(new ServiceProviderOptions
		{
			// Weryfikuje, że KAŻDA rejestracja da się skonstruować (wszystkie
			// zależności konstruktorów są zarejestrowane).
			ValidateOnBuild = true,
			// Włączone, odkąd repozytoria biorą IDbContextFactory zamiast kontekstu
			// Scoped. Wcześniej ta flaga wywalała build kontenera na captive dependency:
			// DbContext o zasięgu Scoped wstrzykiwany do serwisów Singleton.
			ValidateScopes = true
		});

	private static IServiceCollection BuildCollection()
	{
		var services = new ServiceCollection();
		Program.ConfigureServices(services);
		return services;
	}

	[Fact]
	public void ConfigureServices_BuildsWithoutMissingDependencies()
	{
		var wyjatek = Record.Exception(() => Zbuduj().Dispose());

		Assert.Null(wyjatek);
	}

	[Theory]
	[InlineData(typeof(IChatService))]
	[InlineData(typeof(ICodeRunnerService))]
	[InlineData(typeof(IMessageRepository))]
	[InlineData(typeof(ISettingsRepository))]
	[InlineData(typeof(IMessageExportService))]
	[InlineData(typeof(IDialogService))]
	public void Services_AreRegistered(Type typ)
	{
		Assert.Contains(BuildCollection(), d => d.ServiceType == typ);
	}

	[Theory]
	[InlineData(typeof(MainViewModel))]
	[InlineData(typeof(LeftPanelViewModel))]
	[InlineData(typeof(MainChatViewModel))]
	[InlineData(typeof(RightPanelViewModel))]
	[InlineData(typeof(SettingsViewModel))]
	public void ViewModels_AreRegistered(Type typ)
	{
		Assert.Contains(BuildCollection(), d => d.ServiceType == typ);
	}

	[Fact]
	public void ChatCompletionClientFactory_IsRegistered()
	{
		// Bez tej rejestracji ChatService nie da się skonstruować, a aplikacja
		// wywraca się przy pierwszej wysłanej wiadomości.
		Assert.Contains(BuildCollection(),
			d => d.ServiceType == typeof(Func<string, IChatCompletionClient>));
	}

	[Fact]
	public void ChatCompletionClientFactory_ProducesRealClient()
	{
		using var provider = Zbuduj();

		var fabryka = provider.GetRequiredService<Func<string, IChatCompletionClient>>();

		// Samo utworzenie klienta nie wykonuje żadnego żądania sieciowego.
		Assert.IsType<OpenRouterChatClient>(fabryka("klucz-testowy"));
	}

	[Fact]
	public void DbContextFactory_IsRegistered()
	{
		// Uwaga: AddDbContextFactory rejestruje przy okazji sam AIDemonDbContext
		// jako Scoped — to jego normalne zachowanie i nie jest problemem, dopóki
		// nikt nie wstrzykuje kontekstu wprost do Singletona.
		Assert.Contains(BuildCollection(),
			d => d.ServiceType == typeof(IDbContextFactory<AIDemonDbContext>));
	}

	[Theory]
	[InlineData(typeof(MessageRepository))]
	[InlineData(typeof(SettingsRepository))]
	public void Repositories_TakeFactory_NotContextDirectly(Type repozytorium)
	{
		// To jest właściwy niezmiennik: repozytoria są Singletonami, więc przyjęcie
		// kontekstu Scoped w konstruktorze zrobiłoby z niego captive dependency
		// żyjącą przez cały proces. ValidateScopes powyżej złapałoby to przy starcie,
		// ale ten test wskazuje palcem winowajcę.
		var parametry = repozytorium.GetConstructors().Single().GetParameters();

		Assert.DoesNotContain(parametry, p => p.ParameterType == typeof(AIDemonDbContext));
		Assert.Contains(parametry,
			p => p.ParameterType == typeof(IDbContextFactory<AIDemonDbContext>));
	}

	[Fact]
	public void Repositories_AreSingletons()
	{
		using var provider = Zbuduj();

		Assert.Same(provider.GetRequiredService<IMessageRepository>(),
					provider.GetRequiredService<IMessageRepository>());
	}

	[Fact]
	public void Logging_IsRegistered()
	{
		using var provider = Zbuduj();

		Assert.NotNull(provider.GetService<Microsoft.Extensions.Logging.ILogger<ChatService>>());
	}
}
