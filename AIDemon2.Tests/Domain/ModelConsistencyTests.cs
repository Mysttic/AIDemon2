using AIDemon2.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace AIDemon2.Tests.Domain;

/// <summary>
/// Ten test był wcześniej niemożliwy do napisania: seed używał DateTime.UtcNow,
/// więc model zmieniał się przy każdym uruchomieniu, a ostrzeżenie o rozjeździe
/// z migracjami było na stałe wyciszone w OnConfiguring.
/// </summary>
public class ModelConsistencyTests : IDisposable
{
	private readonly SqliteDbFixture _fixture = new();

	[Fact]
	public void Model_HasNoPendingChanges()
	{
		// Wyłapuje sytuację, w której ktoś zmienił encję i zapomniał dodać migracji.
		Assert.False(_fixture.Context.Database.HasPendingModelChanges());
	}

	[Fact]
	public void Seed_IsDeterministic()
	{
		// Dwa niezależne konteksty muszą dać identyczne znaczniki czasu wiersza
		// startowego — inaczej model nie jest stabilny między uruchomieniami.
		using var drugi = new SqliteDbFixture();

		var a = _fixture.Context.Settings.Single();
		var b = drugi.Context.Settings.Single();

		Assert.Equal(a.CreationDate, b.CreationDate);
		Assert.Equal(a.ModificationDate, b.ModificationDate);
	}

	[Fact]
	public void Seed_CreatesSingleSettingsRow()
	{
		var settings = Assert.Single(_fixture.Context.Settings);

		Assert.Equal(1, settings.Id);
		Assert.Equal("You are a helpful assistant.", settings.InstructionPrompt);
		Assert.Equal(string.Empty, settings.ApiKey);
	}

	[Fact]
	public void Migrations_ApplyCleanlyOnEmptyDatabase()
	{
		// Fixture woła Database.Migrate() w konstruktorze; brak wyjątku i obecność
		// tabel oznacza, że migracje wykonują się na czystej bazie.
		Assert.NotEmpty(_fixture.Context.Database.GetAppliedMigrations());
		Assert.Empty(_fixture.Context.Database.GetPendingMigrations());
	}

	[Fact]
	public void AddIsUserMessage_BackfillsAuthorFromProgrammingLanguage()
	{
		// Migracja dodaje kolumnę i uzupełnia ją dla wierszy sprzed zmiany regułą,
		// która obowiązywała wcześniej. Bez tego kroku cała dotychczasowa historia
		// stałaby się po aktualizacji "od AI", bo domyślną wartością kolumny jest 0.
		using var polaczenie = new SqliteConnection("Data Source=:memory:");
		polaczenie.Open();
		var opcje = new DbContextOptionsBuilder<AIDemonDbContext>().UseSqlite(polaczenie).Options;

		using var context = new AIDemonDbContext(opcje);
		var migrator = context.GetService<IMigrator>();
		migrator.Migrate("20250317091632_Initial");

		Wstaw(polaczenie, tresc: "pytanie uzytkownika", jezyk: null);
		Wstaw(polaczenie, tresc: "pusty jezyk tez znaczy uzytkownik", jezyk: "");
		Wstaw(polaczenie, tresc: "odpowiedz ze skryptem", jezyk: "python");

		migrator.Migrate();

		Assert.Equal(new[] { (true, "pytanie uzytkownika"),
							 (true, "pusty jezyk tez znaczy uzytkownik"),
							 (false, "odpowiedz ze skryptem") },
			Autorzy(polaczenie));
	}

	private static void Wstaw(SqliteConnection polaczenie, string tresc, string? jezyk)
	{
		using var polecenie = polaczenie.CreateCommand();
		polecenie.CommandText =
			"INSERT INTO Messages (MessageContent, OriginalMessage, ProgrammingLanguage, " +
			"Favourite, Deleted, CreationDate, ModificationDate) " +
			"VALUES ($tresc, $tresc, $jezyk, 0, 0, '2025-01-01', '2025-01-01');";
		polecenie.Parameters.AddWithValue("$tresc", tresc);
		polecenie.Parameters.AddWithValue("$jezyk", (object?)jezyk ?? DBNull.Value);
		polecenie.ExecuteNonQuery();
	}

	private static List<(bool, string)> Autorzy(SqliteConnection polaczenie)
	{
		using var polecenie = polaczenie.CreateCommand();
		polecenie.CommandText = "SELECT IsUserMessage, MessageContent FROM Messages ORDER BY Id;";
		using var czytnik = polecenie.ExecuteReader();
		var wynik = new List<(bool, string)>();
		while (czytnik.Read())
			wynik.Add((czytnik.GetBoolean(0), czytnik.GetString(1)));
		return wynik;
	}

	public void Dispose() => _fixture.Dispose();
}
