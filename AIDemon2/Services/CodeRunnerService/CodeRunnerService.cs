using AIDemon2.Extensions;
using System.Diagnostics;

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
		// Wybierz interpreter oraz rozszerzenie pliku w zależności od języka
		string interpreter = language.ProgrammingLanguageInterpreter();
		string fileExtension = language.ProgrammingLanguageExtension();
		if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(fileExtension))
			throw new NotSupportedException($"Language '{language}' not supported.");

		// Utwórz tymczasowy plik ze skryptem
		string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + fileExtension);
		string arguments = language.ProgrammingLanguageArguments(tempFile);
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
					? $"{Environment.NewLine}[przerwano: przekroczono limit {Timeout.TotalSeconds:0} s]{Environment.NewLine}"
					: $"{Environment.NewLine}[przerwano przez użytkownika]{Environment.NewLine}");
				return;
			}

			if (process.ExitCode != 0)
				onOutputReceived(
					$"{Environment.NewLine}[proces zakończony kodem {process.ExitCode}]{Environment.NewLine}");
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
