using AIDemon2.Services.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AIDemon2.Tests.Services;

/// <summary>
/// Logger jest potrzebny dokładnie wtedy, gdy coś już poszło źle — więc sam
/// nie może być wtedy źródłem kolejnego wyjątku.
/// </summary>
public class FileLoggerProviderTests : IDisposable
{
	private readonly string _katalog =
		Path.Combine(Path.GetTempPath(), "aidemon2-log-" + Guid.NewGuid().ToString("N"));

	private string TrescLogu() =>
		Directory.GetFiles(_katalog, "*.log").SelectMany(File.ReadAllLines).Aggregate("", (a, b) => a + b + "\n");

	[Fact]
	public void Log_WritesMessageWithLevelAndCategory()
	{
		using var provider = new FileLoggerProvider(_katalog);

		provider.CreateLogger("MojaKategoria").LogWarning("cos podejrzanego");

		string tresc = TrescLogu();
		Assert.Contains("cos podejrzanego", tresc);
		Assert.Contains("MojaKategoria", tresc);
		Assert.Contains("OSTRZ", tresc);
	}

	[Fact]
	public void Log_IncludesExceptionAndStackTrace()
	{
		using var provider = new FileLoggerProvider(_katalog);
		Exception zlapany;
		try
		{
			throw new InvalidOperationException("przyczyna awarii");
		}
		catch (Exception ex)
		{
			zlapany = ex;
		}

		provider.CreateLogger("X").LogError(zlapany, "operacja nie powiodla sie");

		string tresc = TrescLogu();
		Assert.Contains("operacja nie powiodla sie", tresc);
		Assert.Contains("przyczyna awarii", tresc);
		Assert.Contains("InvalidOperationException", tresc);
	}

	[Fact]
	public void Log_RespectsMinimumLevel()
	{
		using var provider = new FileLoggerProvider(_katalog, LogLevel.Warning);
		var logger = provider.CreateLogger("X");

		logger.LogInformation("ma zostac pominiete");
		logger.LogWarning("ma zostac zapisane");

		string tresc = TrescLogu();
		Assert.DoesNotContain("ma zostac pominiete", tresc);
		Assert.Contains("ma zostac zapisane", tresc);
	}

	[Fact]
	public void Log_DoesNotThrow_WhenDirectoryDisappears()
	{
		// Awaria zapisu logu nie może wywrócić aplikacji.
		using var provider = new FileLoggerProvider(_katalog);
		var logger = provider.CreateLogger("X");
		logger.LogInformation("pierwszy wpis");

		Directory.Delete(_katalog, recursive: true);

		var wyjatek = Record.Exception(() => logger.LogError("po usunieciu katalogu"));
		Assert.Null(wyjatek);
	}

	[Fact]
	public void Log_IsSafeFromManyThreads()
	{
		using var provider = new FileLoggerProvider(_katalog);
		var logger = provider.CreateLogger("X");

		Parallel.For(0, 200, i => logger.LogInformation("wpis {Numer}", i));

		Assert.Equal(200, TrescLogu().Split('\n').Count(l => l.Contains("wpis ")));
	}

	[Fact]
	public void DefaultDirectory_IsUnderLocalAppData()
	{
		// Katalog instalacyjny jest tylko do odczytu pod MSIX i w Program Files.
		Assert.StartsWith(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			FileLoggerProvider.DefaultDirectory);
	}

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_katalog))
				Directory.Delete(_katalog, recursive: true);
		}
		catch (Exception)
		{
		}
	}
}
