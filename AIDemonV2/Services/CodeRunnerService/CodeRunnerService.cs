using System.Diagnostics;

public class CodeRunnerService : ICodeRunnerService
{
	public async Task RunCodeAsync(string code, string language, Action<string> onOutputReceived)
	{
		code = RemoveMarkdownCodeBlockMarkers(code);
		// Wybierz interpreter oraz rozszerzenie pliku w zależności od języka
		string interpreter, fileExtension;
		if (language.Equals("python", StringComparison.OrdinalIgnoreCase))
		{
			interpreter = "python"; // upewnij się, że python jest w PATH
			fileExtension = ".py";
		}
		else if (language.Equals("powershell", StringComparison.OrdinalIgnoreCase))
		{
			interpreter = "powershell"; // lub "powershell"
			fileExtension = ".ps1";
		}
		else
		{
			throw new NotSupportedException($"Language '{language}' not supported.");
		}

		// Utwórz tymczasowy plik ze skryptem
		string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + fileExtension);
		await File.WriteAllTextAsync(tempFile, code);

		var psi = new ProcessStartInfo
		{
			FileName = interpreter,
			Arguments = tempFile,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		using var process = new Process { StartInfo = psi };
		process.OutputDataReceived += (s, e) =>
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				onOutputReceived(e.Data + Environment.NewLine);
			}
		};
		process.ErrorDataReceived += (s, e) =>
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				onOutputReceived(e.Data + Environment.NewLine);
			}
		};

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		await process.WaitForExitAsync();
		File.Delete(tempFile);
	}

	private string RemoveMarkdownCodeBlockMarkers(string code)
	{
		// Usuń otwierający marker, jeśli istnieje
		if (code.StartsWith("```"))
		{
			int firstNewline = code.IndexOf('\n');
			if (firstNewline >= 0)
			{
				code = code.Substring(firstNewline + 1);
			}
		}
		// Usuń zamykający marker, jeśli istnieje
		if (code.EndsWith("```"))
		{
			int lastNewline = code.LastIndexOf('\n');
			if (lastNewline >= 0)
			{
				code = code.Substring(0, lastNewline);
			}
		}
		return code;
	}
}