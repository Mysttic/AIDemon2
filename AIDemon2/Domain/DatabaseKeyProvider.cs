using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace AIDemon2.Domain;

/// <summary>
/// Dostarcza klucz szyfrujący bazę SQLCipher.
///
/// Do wersji 1.0.18 hasło było wpisane w <c>Properties/Resources.resx</c>, więc trafiało
/// do każdej wydanej binarki i do historii repozytorium. Szyfrowanie było przez to pozorne:
/// jedno publicznie znane hasło otwierało bazy wszystkich użytkowników, a w bazie leży
/// klucz API do usługi AI. Teraz każda instalacja dostaje własny losowy klucz, trzymany
/// obok bazy i zaszyfrowany mechanizmem DPAPI systemu Windows.
///
/// Stara baza jest przy pierwszym uruchomieniu przekluczana (<c>PRAGMA rekey</c>), więc
/// aktualizacja nie gubi historii rozmów.
/// </summary>
internal static class DatabaseKeyProvider
{
	/// <summary>
	/// Hasło z wydań do 1.0.18 włącznie. NIE jest już sekretem — jest w historii gita
	/// i w każdej wydanej binarce. Zostaje wyłącznie po to, żeby otworzyć starą bazę
	/// i przekluczyć ją na klucz właściwy dla tej instalacji.
	/// </summary>
	private const string LegacyPassword = "P@ssword1";

	private const string KeyFileName = "AIDemon2.key";
	private const string BackupSuffix = ".pre-rekey.bak";

	/// <summary>
	/// Dodatkowa entropia DPAPI. Nie jest sekretem — chroni przed odszyfrowaniem blobu
	/// przez inną aplikację, która przypadkiem użyłaby DPAPI w tym samym zakresie.
	/// </summary>
	private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AIDemon2.DatabaseKey.v1");

	/// <summary>
	/// Zakres DPAPI. CurrentUser, odkąd baza leży w %LOCALAPPDATA% i każde konto ma
	/// własną — klucz jest nie do odczytania przez innych użytkowników tej maszyny.
	/// </summary>
	private const DataProtectionScope Scope = DataProtectionScope.CurrentUser;

	/// <summary>
	/// Zakres używany, gdy baza leżała w katalogu instalacyjnym i była wspólna dla
	/// całej maszyny. Klucze z tamtego okresu trzeba jeszcze umieć odczytać.
	/// </summary>
	private const DataProtectionScope LegacyScope = DataProtectionScope.LocalMachine;

	private static readonly object Gate = new();

	/// <summary>
	/// Zwraca klucz dla wskazanej bazy, tworząc go przy pierwszym uruchomieniu
	/// i przekluczając bazę odziedziczoną po starych wersjach.
	/// </summary>
	public static string GetOrCreate(string databasePath)
	{
		lock (Gate)
		{
			string keyPath = GetKeyPath(databasePath);

			if (File.Exists(keyPath))
			{
				string existing = Unprotect(keyPath, out bool wymagaPodniesienia);
				if (wymagaPodniesienia)
					Protect(keyPath, existing);   // przepisz na zakres bieżącego użytkownika
				return existing;
			}

			string newKey = GenerateKey();

			if (File.Exists(databasePath))
				RekeyLegacyDatabase(databasePath, newKey);

			Protect(keyPath, newKey);
			return newKey;
		}
	}

	public static string GetKeyPath(string databasePath) =>
		Path.Combine(Path.GetDirectoryName(databasePath) ?? string.Empty, KeyFileName);

	/// <summary>
	/// 32 bajty z generatora kryptograficznego, zakodowane Base64. Base64 nie zawiera
	/// apostrofu, więc klucz da się bezpiecznie wstawić do <c>PRAGMA rekey = '...'</c>,
	/// gdzie SQLite nie przyjmuje parametrów.
	/// </summary>
	private static string GenerateKey() =>
		Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

	/// <summary>
	/// Otwiera bazę starym, publicznie znanym hasłem i przekluczą ją na nowy klucz.
	/// Przed operacją odkłada kopię pliku — <c>PRAGMA rekey</c> przepisuje wszystkie
	/// strony bazy i przerwanie go w połowie kosztowałoby użytkownika całą historię.
	/// </summary>
	private static void RekeyLegacyDatabase(string databasePath, string newKey)
	{
		string backupPath = databasePath + BackupSuffix;
		File.Copy(databasePath, backupPath, overwrite: true);

		try
		{
			using (var connection = new SqliteConnection(BuildConnectionString(databasePath, LegacyPassword)))
			{
				connection.Open();

				using var command = connection.CreateCommand();
				// PRAGMA nie przyjmuje parametrów; klucz jest Base64, więc nie zawiera
				// apostrofu i nie da się przez niego wyjść z literału.
				command.CommandText = $"PRAGMA rekey = '{newKey}';";
				command.ExecuteNonQuery();
			}

			// Bez tego pula oddałaby połączenie otwarte jeszcze starym kluczem.
			SqliteConnection.ClearAllPools();

			VerifyKeyOpensDatabase(databasePath, newKey);
		}
		catch (Exception ex)
		{
			SqliteConnection.ClearAllPools();
			RestoreBackup(backupPath, databasePath);

			throw new InvalidOperationException(
				"Nie udało się przekluczyć bazy AIDemon2 na klucz właściwy dla tej instalacji. " +
				$"Baza została przywrócona ze stanu sprzed operacji. Szczegóły: {ex.Message}", ex);
		}

		TryDelete(backupPath);
	}

	/// <summary>Sprawdza, że po przekluczeniu baza faktycznie otwiera się nowym kluczem.</summary>
	private static void VerifyKeyOpensDatabase(string databasePath, string key)
	{
		using var connection = new SqliteConnection(BuildConnectionString(databasePath, key));
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = "SELECT count(*) FROM sqlite_master;";
		command.ExecuteScalar();
	}

	public static string BuildConnectionString(string databasePath, string key) =>
		new SqliteConnectionStringBuilder
		{
			DataSource = databasePath,
			Password = key
		}.ToString();

	private static void Protect(string keyPath, string key)
	{
		byte[] blob = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Entropy, Scope);

		// Zapis przez plik tymczasowy: przerwanie w połowie nie zostawi obciętego
		// klucza, którym nie da się już otworzyć bazy.
		string tempPath = keyPath + ".tmp";
		File.WriteAllBytes(tempPath, blob);
		File.Move(tempPath, keyPath, overwrite: true);
	}

	/// <param name="wymagaPodniesienia">
	/// true, gdy klucz dało się odczytać dopiero starym zakresem — trzeba go wtedy
	/// zapisać ponownie, już jako klucz bieżącego użytkownika.
	/// </param>
	private static string Unprotect(string keyPath, out bool wymagaPodniesienia)
	{
		byte[] blob = File.ReadAllBytes(keyPath);
		try
		{
			wymagaPodniesienia = false;
			return Encoding.UTF8.GetString(ProtectedData.Unprotect(blob, Entropy, Scope));
		}
		catch (CryptographicException)
		{
			wymagaPodniesienia = true;
			return Encoding.UTF8.GetString(ProtectedData.Unprotect(blob, Entropy, LegacyScope));
		}
	}

	private static void RestoreBackup(string backupPath, string databasePath)
	{
		try
		{
			if (File.Exists(backupPath))
				File.Copy(backupPath, databasePath, overwrite: true);
		}
		catch (Exception)
		{
			// Kopia zostaje na dysku — użytkownik może ją odzyskać ręcznie.
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception)
		{
			// Kopia zapasowa nie przeszkadza w działaniu; skasuje ją kolejne uruchomienie.
		}
	}
}
