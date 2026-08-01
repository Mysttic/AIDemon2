using AIDemon2.Extensions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AIDemon2.Services.CodeRunnerService;

public class CodeRunnerService : ICodeRunnerService
{
	/// <summary>
	/// Limit czasu wykonania skryptu. Uruchamiany kod pochodzi od modelu AI,
	/// więc pętla nieskończona jest scenariuszem realnym, nie teoretycznym.
	/// Ustawiane przez inicjalizator obiektu (DI używa konstruktora bezparametrowego).
	/// </summary>
	public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

	public async Task RunCodeAsync(string code, string language, Action<string> onOutputReceived,
		CancellationToken cancellationToken = default)
	{
		code = code.RemoveMarkdownCodeBlockMarkers();
		string fileExtension = language.ProgrammingLanguageExtension();
		if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(fileExtension))
			throw new NotSupportedException($"Language '{language}' not supported.");

		if (!language.IsSupportedOnThisPlatform())
			throw new NotSupportedException(language.UnsupportedReason());

		// PHP bez znacznika otwierającego wypisuje własne źródło i kończy z kodem 0,
		// więc bez tego kroku aplikacja uznałaby taki przebieg za udany.
		code = code.ApplyPreamble(language);
		// Powłoki uniksowe przerywają na plikach z CRLF, a Windows zapisuje je domyślnie.
		code = code.NormalizeLineEndings(language);

		string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + fileExtension);

		// Interpreter rozwiązywany PRZED utworzeniem pliku i poza wątkiem UI: samo
		// wyszukiwanie przechodzi cały PATH, a dla powłok uniksowych potrafi uruchomić
		// wsl.exe. Gdyby to szło po zapisie, nieudane wyszukanie zostawiałoby
		// osierocony plik w katalogu tymczasowym.
		var (interpreter, arguments) = await Task.Run(
			() => ResolveLauncher(language, tempFile), cancellationToken);

		await File.WriteAllTextAsync(tempFile, code, cancellationToken);

		var psi = new ProcessStartInfo
		{
			FileName = interpreter,
			Arguments = arguments,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		using var process = new Process { StartInfo = psi };
		// e.Data == null oznacza koniec strumienia; pusta linia to prawidłowe wyjście
		// programu i nie wolno jej pomijać.
		process.OutputDataReceived += (s, e) =>
		{
			if (e.Data is not null)
				onOutputReceived(e.Data + Environment.NewLine);
		};
		process.ErrorDataReceived += (s, e) =>
		{
			if (e.Data is not null)
				onOutputReceived(e.Data + Environment.NewLine);
		};

		using var timeoutSource = new CancellationTokenSource(Timeout);
		using var linkedSource =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

		try
		{
			process.Start();

			// Bez tych dwóch wywołań handlery zarejestrowane wyżej NIGDY nie zostaną
			// wywołane, a proces piszący więcej niż bufor pipe'a (~4 KB) zawiesza się
			// na stałe, bo nikt nie czyta jego strumieni.
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			try
			{
				// WaitForExitAsync czeka też na opróżnienie strumieni, więc czytanie
				// ich po zakończeniu procesu (ReadToEndAsync) jest zbędne i było
				// właśnie źródłem zakleszczenia.
				await process.WaitForExitAsync(linkedSource.Token);
			}
			catch (OperationCanceledException)
			{
				KillProcessTree(process);
				onOutputReceived(timeoutSource.IsCancellationRequested
					? $"{Environment.NewLine}[stopped: exceeded the {Timeout.TotalSeconds:0} s limit]{Environment.NewLine}"
					: $"{Environment.NewLine}[cancelled]{Environment.NewLine}");
				return;
			}

			if (process.ExitCode != 0)
				onOutputReceived(
					$"{Environment.NewLine}[process exited with code {process.ExitCode}]{Environment.NewLine}");
		}
		catch (Exception ex)
		{
			onOutputReceived(ex.Message);
		}
		finally
		{
			// Proces mógł przeżyć wyjątek rzucony między Start() a zakończeniem —
			// bez tego zostałby sierotą działającą po zamknięciu aplikacji.
			KillProcessTree(process);
			TryDeleteFile(tempFile);
		}
	}

	/// <summary>
	/// Wybiera interpreter i argumenty. Zwraca pierwszego kandydata, którego udało się
	/// odnaleźć — nazwy binarek bywają różne (pwsh/powershell, lua/lua5.4/luajit),
	/// a na Windows część z nich to pliki .bat, których CreateProcess nie znajdzie
	/// bez jawnego rozszerzenia.
	/// </summary>
	private static (string Interpreter, string Arguments) ResolveLauncher(string language, string scriptPath)
	{
		var platforma = language.ForCurrentPlatform();

		// Bash na Windows nie ma stałej nazwy: Git Bash nie jest na PATH, a "bash"
		// wskazuje launcher WSL. Trzeba go odnaleźć i przetłumaczyć ścieżkę skryptu.
		if (platforma?.Shell == "posix")
		{
			var powloka = PosixShellLocator.Locate()
				?? throw new NotSupportedException(
					"No POSIX shell found. Install Git for Windows or WSL to run bash scripts.");

			return (powloka.Path, PosixShellLocator.BuildArguments(powloka, scriptPath));
		}

		string argumenty = language.ProgrammingLanguageArguments(scriptPath);
		var kandydaci = language.ProgrammingLanguageLaunchers();

		foreach (string kandydat in kandydaci)
			if (LauncherIstnieje(kandydat))
				return (kandydat, argumenty);

		// Zapasowa lokalizacja w katalogu Git for Windows (tam mieszka m.in. perl).
		if (!string.IsNullOrEmpty(platforma?.GitBashFallback)
			&& PosixShellLocator.Locate() is { Kind: PosixShellKind.GitBash } git)
		{
			// git.Path to <Git>\bin\bash.exe — cofamy się do korzenia instalacji.
			string? korzen = Path.GetDirectoryName(Path.GetDirectoryName(git.Path));
			if (korzen is not null)
			{
				string pelna = Path.Combine(korzen, platforma.GitBashFallback.Replace('/', Path.DirectorySeparatorChar));
				if (File.Exists(pelna))
					return (pelna, argumenty);
			}
		}

		// Żaden kandydat się nie znalazł. Bez tego rzutu użytkownik dostawał surowy
		// komunikat Win32 ("An error occurred trying to start process 'groovy.bat'"),
		// z którego nie wynika, że po prostu brakuje interpretera.
		throw new NotSupportedException(
			$"No interpreter found for '{language}'. Names tried: {string.Join(", ", kandydaci)}. " +
			"Install one and make sure it is available on your PATH.");
	}

	/// <summary>
	/// Czy da się uruchomić binarkę o tej nazwie. Sprawdza PATH ręcznie, bo
	/// <see cref="Process"/> z <c>UseShellExecute=false</c> nie korzysta z PATHEXT —
	/// dopisuje wyłącznie ".exe", więc launcher w postaci .bat/.cmd byłby pominięty.
	/// </summary>
	private static bool LauncherIstnieje(string nazwa)
	{
		if (Path.IsPathRooted(nazwa))
			return File.Exists(nazwa);

		bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		string[] rozszerzenia = windows && !Path.HasExtension(nazwa)
			? new[] { ".exe", ".bat", ".cmd" }
			: new[] { string.Empty };

		foreach (string katalog in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
					.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			foreach (string rozszerzenie in rozszerzenia)
			{
				string kandydat;
				try
				{
					kandydat = Path.Combine(katalog.Trim('"'), nazwa + rozszerzenie);
				}
				catch (ArgumentException)
				{
					// Wpis w PATH z niedozwolonymi znakami — pomijamy go.
					continue;
				}

				if (!File.Exists(kandydat))
					continue;

				// Aplikacje ze Sklepu Microsoft zostawiają w PATH zerobajtowe zaślepki,
				// które przy uruchomieniu kończą się kodem 9009 zamiast startem programu.
				if (windows && new FileInfo(kandydat).Length == 0)
					continue;

				return true;
			}
		}
		return false;
	}

	private static void KillProcessTree(Process process)
	{
		try
		{
			if (!process.HasExited)
				process.Kill(entireProcessTree: true);
		}
		catch (Exception)
		{
			// Proces mógł zakończyć się między sprawdzeniem a zabiciem, albo nigdy
			// nie wystartował. Żaden z tych przypadków nie jest błędem.
		}
	}

	private static void TryDeleteFile(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception)
		{
			// Interpreter mógł jeszcze trzymać plik otwarty. Katalog tymczasowy
			// i tak jest sprzątany przez system — to nie powód, by przerywać.
		}
	}
}
