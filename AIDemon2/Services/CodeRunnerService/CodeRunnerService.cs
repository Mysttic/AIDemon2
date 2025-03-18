using AIDemon2.Extensions;
using System.Diagnostics;

public class CodeRunnerService : ICodeRunnerService
{
	public async Task RunCodeAsync(string code, string language, Action<string> onOutputReceived)
	{
		code = code.RemoveMarkdownCodeBlockMarkers();
		// Wybierz interpreter oraz rozszerzenie pliku w zależności od języka
		string interpreter = language.ProgrammingLanguageInterpreter();
		string fileExtension = language.ProgrammingLanguageExtension();
		if(string.IsNullOrEmpty(language) || string.IsNullOrEmpty(fileExtension))
			throw new NotSupportedException($"Language '{language}' not supported.");

		// Utwórz tymczasowy plik ze skryptem
		string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + fileExtension);
		string arguments = language.ProgrammingLanguageArguments(tempFile);
		await File.WriteAllTextAsync(tempFile, code);

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

		try
		{
			process.Start();
			await process.WaitForExitAsync();

			string output = await process.StandardOutput.ReadToEndAsync();
			string error = await process.StandardError.ReadToEndAsync();

			if (!string.IsNullOrEmpty(output))
				onOutputReceived(output);
			if (!string.IsNullOrEmpty(error))
				onOutputReceived(error);
		}
		catch (Exception ex)
		{
			onOutputReceived(ex.Message);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

}