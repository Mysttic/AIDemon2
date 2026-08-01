namespace AIDemon2.Domain;

/// <summary>
/// Jedno miejsce ustalające, gdzie leży baza i jakim kluczem się ją otwiera.
///
/// Baza mieszka w <c>%LOCALAPPDATA%\AIDemon2\</c>. Wcześniej powstawała w katalogu
/// instalacyjnym, co dawało dwa problemy naraz:
///   * pod MSIX i przy instalacji do Program Files katalog jest tylko do odczytu,
///     więc aplikacja wywracała się przy starcie, zanim pokazała okno,
///   * jedna baza była wspólna dla wszystkich kont na maszynie — historia rozmów
///     i klucz API jednego użytkownika były widoczne dla pozostałych.
///
/// Istniejąca baza jest przy pierwszym uruchomieniu KOPIOWANA do nowej lokalizacji.
/// Kopiowana, nie przenoszona: na maszynie wielu użytkowników każde konto ma dostać
/// własną kopię dotychczasowej historii, a nie odebrać ją pozostałym.
/// </summary>
internal static class DatabaseLocation
{
	private const string DatabaseFileName = "AIDemon2.db";
	private const string AppFolderName = "AIDemon2";

	/// <summary>Katalog danych użytkownika — zawsze zapisywalny.</summary>
	public static string DataDirectory => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		AppFolderName);

	public static string DatabasePath => Path.Combine(DataDirectory, DatabaseFileName);

	/// <summary>Lokalizacja z wersji ≤ 1.0.18 — katalog instalacyjny.</summary>
	public static string LegacyDatabasePath =>
		Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFileName);

	/// <summary>
	/// Connection string z kluczem właściwym dla tej instalacji. Przygotowuje też
	/// katalog danych i przenosi bazę z poprzedniej lokalizacji, jeśli trzeba.
	/// </summary>
	public static string ConnectionString
	{
		get
		{
			PrepareDataDirectory();
			return DatabaseKeyProvider.BuildConnectionString(
				DatabasePath,
				DatabaseKeyProvider.GetOrCreate(DatabasePath));
		}
	}

	/// <summary>
	/// Tworzy katalog danych i jednorazowo kopiuje bazę oraz plik klucza
	/// ze starej lokalizacji. Idempotentne.
	/// </summary>
	public static void PrepareDataDirectory() =>
		PrepareDataDirectory(DatabasePath, LegacyDatabasePath);

	/// <summary>
	/// Wariant z jawnymi ścieżkami — operacja dotyka danych użytkownika, więc musi
	/// dać się sprawdzić testem bez ruszania prawdziwego profilu.
	/// </summary>
	internal static void PrepareDataDirectory(string databasePath, string legacyDatabasePath)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

		if (File.Exists(databasePath) || !File.Exists(legacyDatabasePath))
			return;

		// Klucz musi wywędrować razem z bazą — bez niego skopiowany plik
		// byłby nie do otwarcia.
		string legacyKey = DatabaseKeyProvider.GetKeyPath(legacyDatabasePath);
		string targetKey = DatabaseKeyProvider.GetKeyPath(databasePath);

		File.Copy(legacyDatabasePath, databasePath, overwrite: false);
		if (File.Exists(legacyKey) && !File.Exists(targetKey))
			File.Copy(legacyKey, targetKey, overwrite: false);
	}
}
