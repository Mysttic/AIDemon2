using AIDemon2.Domain;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace AIDemon2.Tests.Domain;

/// <summary>
/// Przeniesienie bazy z katalogu instalacyjnego do %LOCALAPPDATA% dotyka danych
/// użytkownika — historii rozmów i klucza API. Każda ścieżka aktualizacji musi być
/// sprawdzona, bo błąd oznacza utratę danych, a nie tylko brzydki komunikat.
/// </summary>
public class DatabaseMigrationTests : IDisposable
{
	private const string SpaloneHaslo = "P@ssword1";

	private readonly string _stary = Utworz("stary");
	private readonly string _nowy = Utworz("nowy");

	private static string Utworz(string nazwa)
	{
		string sciezka = Path.Combine(Path.GetTempPath(),
			$"aidemon2-{nazwa}-{Guid.NewGuid():N}");
		Directory.CreateDirectory(sciezka);
		return sciezka;
	}

	private string StaraBaza => Path.Combine(_stary, "AIDemon2.db");
	private string NowaBaza => Path.Combine(_nowy, "AIDemon2.db");

	private static void UtworzBaze(string sciezka, string haslo, string tresc)
	{
		using var connection = new SqliteConnection(
			DatabaseKeyProvider.BuildConnectionString(sciezka, haslo));
		connection.Open();
		using var command = connection.CreateCommand();
		command.CommandText =
			$"CREATE TABLE Historia(Tresc TEXT); INSERT INTO Historia VALUES ('{tresc}');";
		command.ExecuteNonQuery();
		SqliteConnection.ClearAllPools();
	}

	private static string OdczytajHistorie(string sciezka, string klucz)
	{
		using var connection = new SqliteConnection(
			DatabaseKeyProvider.BuildConnectionString(sciezka, klucz));
		connection.Open();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT Tresc FROM Historia;";
		var wynik = (string)command.ExecuteScalar()!;
		SqliteConnection.ClearAllPools();
		return wynik;
	}

	[Fact]
	public void Prepare_CopiesDatabaseAndKey_FromLegacyLocation()
	{
		UtworzBaze(StaraBaza, SpaloneHaslo, "historia z poprzedniej wersji");
		string klucz = DatabaseKeyProvider.GetOrCreate(StaraBaza);

		DatabaseLocation.PrepareDataDirectory(NowaBaza, StaraBaza);

		Assert.True(File.Exists(NowaBaza));
		Assert.True(File.Exists(DatabaseKeyProvider.GetKeyPath(NowaBaza)),
			"klucz musi wywędrować razem z bazą, inaczej kopia jest nie do otwarcia");
		Assert.Equal("historia z poprzedniej wersji", OdczytajHistorie(NowaBaza, klucz));
	}

	[Fact]
	public void Prepare_LeavesLegacyDatabaseInPlace()
	{
		// Kopiujemy, nie przenosimy: na maszynie wielu użytkowników każde konto ma
		// dostać własną kopię, a nie odebrać bazę pozostałym.
		UtworzBaze(StaraBaza, SpaloneHaslo, "x");
		DatabaseKeyProvider.GetOrCreate(StaraBaza);

		DatabaseLocation.PrepareDataDirectory(NowaBaza, StaraBaza);

		Assert.True(File.Exists(StaraBaza));
	}

	[Fact]
	public void Prepare_DoesNothing_WhenTargetAlreadyExists()
	{
		UtworzBaze(StaraBaza, SpaloneHaslo, "stara tresc");
		DatabaseKeyProvider.GetOrCreate(StaraBaza);
		UtworzBaze(NowaBaza, "inny-klucz", "biezaca tresc");

		DatabaseLocation.PrepareDataDirectory(NowaBaza, StaraBaza);

		Assert.Equal("biezaca tresc", OdczytajHistorie(NowaBaza, "inny-klucz"));
	}

	[Fact]
	public void Prepare_IsIdempotent()
	{
		UtworzBaze(StaraBaza, SpaloneHaslo, "tresc");
		string klucz = DatabaseKeyProvider.GetOrCreate(StaraBaza);

		DatabaseLocation.PrepareDataDirectory(NowaBaza, StaraBaza);
		DatabaseLocation.PrepareDataDirectory(NowaBaza, StaraBaza);

		Assert.Equal("tresc", OdczytajHistorie(NowaBaza, klucz));
	}

	[Fact]
	public void Prepare_CreatesDirectory_OnFreshInstall()
	{
		string swiezy = Path.Combine(_nowy, "podkatalog", "AIDemon2.db");

		DatabaseLocation.PrepareDataDirectory(swiezy, Path.Combine(_stary, "nie-istnieje.db"));

		Assert.True(Directory.Exists(Path.GetDirectoryName(swiezy)));
		Assert.False(File.Exists(swiezy));
	}

	[Fact]
	public void Key_IsUpgradedFromMachineScopeToUserScope()
	{
		// Klucz zapisany, gdy baza była wspólna dla maszyny, musi dać się odczytać
		// po aktualizacji — i zostać przepisany na zakres bieżącego użytkownika.
		UtworzBaze(NowaBaza, "klucz-z-poprzedniej-wersji", "historia");
		string keyPath = DatabaseKeyProvider.GetKeyPath(NowaBaza);
		File.WriteAllBytes(keyPath, ProtectedData.Protect(
			Encoding.UTF8.GetBytes("klucz-z-poprzedniej-wersji"),
			Encoding.UTF8.GetBytes("AIDemon2.DatabaseKey.v1"),
			DataProtectionScope.LocalMachine));

		string odczytany = DatabaseKeyProvider.GetOrCreate(NowaBaza);

		Assert.Equal("klucz-z-poprzedniej-wersji", odczytany);
		// Po podniesieniu blob musi dać się odczytać zakresem użytkownika.
		byte[] blob = File.ReadAllBytes(keyPath);
		string poPodniesieniu = Encoding.UTF8.GetString(ProtectedData.Unprotect(
			blob, Encoding.UTF8.GetBytes("AIDemon2.DatabaseKey.v1"),
			DataProtectionScope.CurrentUser));
		Assert.Equal("klucz-z-poprzedniej-wersji", poPodniesieniu);
	}

	[Fact]
	public void DataDirectory_IsUnderLocalAppData()
	{
		Assert.StartsWith(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			DatabaseLocation.DataDirectory);
	}

	public void Dispose()
	{
		SqliteConnection.ClearAllPools();
		foreach (var katalog in new[] { _stary, _nowy })
		{
			try
			{
				Directory.Delete(katalog, recursive: true);
			}
			catch (Exception)
			{
			}
		}
	}
}
