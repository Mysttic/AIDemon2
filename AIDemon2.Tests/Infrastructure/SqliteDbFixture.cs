using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AIDemon2.Tests.Infrastructure;

/// <summary>
/// Baza SQLite w pamięci, na realnych migracjach projektu.
///
/// Celowo NIE używamy providera InMemory: nie wykonuje on SQL-a, więc nie złapałby
/// ani błędów w filtrach zapytań, ani gubienia <see cref="DateTimeKind"/> przy zapisie
/// i odczycie — a to są dokładnie te klasy defektów, które te testy mają wykrywać.
///
/// Połączenie musi pozostać otwarte przez cały czas życia obiektu: SQLite kasuje bazę
/// „:memory:" w chwili zamknięcia ostatniego połączenia. Wszystkie konteksty tworzone
/// przez fabrykę współdzielą to jedno połączenie, więc widzą tę samą bazę.
///
/// Implementuje <see cref="IDbContextFactory{TContext}"/>, żeby dało się go podać
/// wprost do repozytoriów — dokładnie tak, jak robi to kontener w aplikacji.
/// </summary>
public sealed class SqliteDbFixture : IDisposable, IDbContextFactory<AIDemonDbContext>
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<AIDemonDbContext> _options;

	/// <summary>Kontekst do asercji w testach — niezależny od tych, których używa kod produkcyjny.</summary>
	public AIDemonDbContext Context { get; }

	public SqliteDbFixture()
	{
		_connection = new SqliteConnection("Data Source=:memory:");
		_connection.Open();

		_options = new DbContextOptionsBuilder<AIDemonDbContext>()
			.UseSqlite(_connection)
			.Options;

		Context = new AIDemonDbContext(_options);
		Context.Database.Migrate();
	}

	public AIDemonDbContext CreateDbContext() => new(_options);

	/// <summary>
	/// Odłącza wszystkie encje od change trackera kontekstu asercji. Bez tego
	/// asercje widzą instancje doklejone przez fixup nawigacji, a nie to,
	/// co realnie zwróciło zapytanie.
	/// </summary>
	public void Detach() => Context.ChangeTracker.Clear();

	public void Dispose()
	{
		Context.Dispose();
		_connection.Dispose();
	}
}
