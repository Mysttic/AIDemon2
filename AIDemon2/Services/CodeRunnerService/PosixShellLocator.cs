using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace AIDemon2.Services.CodeRunnerService;

/// <summary>Gdzie znaleziono powłokę i jak podać jej ścieżkę do skryptu.</summary>
public sealed record PosixShell(string Path, PosixShellKind Kind)
{
	/// <summary>
	/// Tłumaczy ścieżkę Windows na postać zrozumiałą dla danej powłoki.
	/// To nie jest kosmetyka: WSL nie widzi <c>C:\Users\...</c> w ogóle — jego katalogi
	/// systemu gospodarza leżą pod <c>/mnt/c/...</c>. Bez tłumaczenia skrypt
	/// nie uruchomi się mimo poprawnie odnalezionego interpretera.
	/// </summary>
	public string TranslatePath(string windowsPath) => Kind switch
	{
		// Git Bash rozumie także zapis windowsowy, ale forma /c/... jest jednoznaczna.
		PosixShellKind.GitBash => ToUnix(windowsPath, "/"),
		PosixShellKind.Wsl => ToUnix(windowsPath, "/mnt/"),
		_ => windowsPath
	};

	private static string ToUnix(string windowsPath, string prefix)
	{
		string sciezka = windowsPath.Replace('\\', '/');
		if (sciezka.Length >= 2 && sciezka[1] == ':')
			return prefix + char.ToLowerInvariant(sciezka[0]) + sciezka[2..];
		return sciezka;
	}
}

public enum PosixShellKind
{
	GitBash,
	Wsl
}

/// <summary>
/// Odnajduje powłokę uniksową na Windows.
///
/// Powód istnienia: launcher "bash" na Windows NIE trafia w Git Bash. Git for Windows
/// celowo nie dodaje swojego <c>bin</c> do systemowego PATH, a <c>C:\Windows\System32\bash.exe</c>
/// to launcher WSL. Uruchomienie skryptu przez samą nazwę "bash" kończyło się więc
/// błędem WSL albo brakiem pliku — i to jest prawdziwy powód, dla którego bash figurował
/// w dokumentacji jako niedokończony.
/// </summary>
public static class PosixShellLocator
{
	/// <summary>Znajduje powłokę, preferując Git Bash. Zwraca null, gdy nie ma żadnej.</summary>
	public static PosixShell? Locate()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return new PosixShell("bash", PosixShellKind.GitBash);

		foreach (string katalog in GitInstallRoots())
		{
			string kandydat = Path.Combine(katalog, "bin", "bash.exe");
			if (File.Exists(kandydat))
				return new PosixShell(kandydat, PosixShellKind.GitBash);
		}

		string wsl = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.System), "wsl.exe");
		if (File.Exists(wsl) && WslMaDystrybucje())
			return new PosixShell(wsl, PosixShellKind.Wsl);

		return null;
	}

	/// <summary>Argumenty uruchomienia skryptu w znalezionej powłoce.</summary>
	public static string BuildArguments(PosixShell shell, string windowsScriptPath)
	{
		string sciezka = shell.TranslatePath(windowsScriptPath);
		return shell.Kind == PosixShellKind.Wsl
			// wsl.exe sam nie uruchomi pliku — musi dostać polecenie do wykonania.
			? $"bash \"{sciezka}\""
			: $"\"{sciezka}\"";
	}

	private static IEnumerable<string> GitInstallRoots()
	{
		foreach (string zRejestru in GitRootsFromRegistry())
			yield return zRejestru;

		yield return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git");
		yield return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git");
		yield return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Git");
	}

	[SupportedOSPlatform("windows")]
	private static List<string> GitRootsFromRegistry()
	{
		var wynik = new List<string>();
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return wynik;

		foreach (var (gniazdo, widok) in new[]
		{
			(RegistryHive.LocalMachine, RegistryView.Registry64),
			(RegistryHive.LocalMachine, RegistryView.Registry32),
			(RegistryHive.CurrentUser, RegistryView.Default)
		})
		{
			try
			{
				using var baza = RegistryKey.OpenBaseKey(gniazdo, widok);
				using var klucz = baza.OpenSubKey(@"SOFTWARE\GitForWindows");
				if (klucz?.GetValue("InstallPath") is string sciezka && !string.IsNullOrWhiteSpace(sciezka))
					wynik.Add(sciezka);
			}
			catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
			{
				// Brak dostępu do gałęzi rejestru nie może przerwać wyszukiwania —
				// zostają jeszcze ścieżki domyślne.
			}
		}
		return wynik;
	}

	/// <summary>
	/// wsl.exe istnieje na każdym Windows 10/11, także bez zainstalowanej dystrybucji —
	/// wtedy uruchomienie czegokolwiek kończy się błędem. Sprawdzamy więc, czy jest
	/// co uruchamiać, zamiast obiecywać obsługę basha, której nie ma.
	/// </summary>
	private static bool WslMaDystrybucje()
	{
		try
		{
			using var proces = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = "wsl.exe",
				Arguments = "--list --quiet",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			});
			if (proces is null)
				return false;

			// Odczyt strumienia MUSI iść równolegle z oczekiwaniem: ReadToEnd() przed
			// WaitForExit blokuje aż do zamknięcia strumienia, więc deklarowany limit
			// 3 s nie ograniczałby niczego, gdyby wsl.exe zawisł.
			var odczyt = proces.StandardOutput.ReadToEndAsync();
			if (!proces.WaitForExit(3000))
			{
				proces.Kill(entireProcessTree: true);
				return false;
			}
			string wyjscie = odczyt.GetAwaiter().GetResult();

			// wsl.exe --list wypisuje UTF-16, co po odczycie jako UTF-8 daje bajty zerowe.
			return proces.ExitCode == 0 && !string.IsNullOrWhiteSpace(wyjscie.Replace("\0", string.Empty));
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
		{
			return false;
		}
	}
}
