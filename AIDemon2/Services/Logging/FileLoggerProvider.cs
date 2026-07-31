using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace AIDemon2.Services.Logging;

/// <summary>
/// Zapisuje logi do pliku w <c>%LOCALAPPDATA%\AIDemon2\logs\</c>.
///
/// Aplikacja nie miała dotąd żadnego logowania: dwa bloki <c>catch</c> w całym projekcie,
/// zero globalnych handlerów wyjątków. Każdy błąd albo znikał bez śladu, albo ubijał
/// proces bez komunikatu, przez co diagnostyka zgłoszenia użytkownika była niemożliwa.
///
/// Celowo bez Serilog i innych zależności — potrzebny jest plik z datą, poziomem
/// i stosem wywołań, a nie system logowania.
///
/// %LOCALAPPDATA%, a nie katalog instalacyjny: ten drugi jest tylko do odczytu
/// pod MSIX i przy instalacji do Program Files.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
	private readonly string _directory;
	private readonly LogLevel _minimumLevel;
	private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
	private readonly object _writeGate = new();

	public FileLoggerProvider(LogLevel minimumLevel = LogLevel.Information)
		: this(DefaultDirectory, minimumLevel)
	{
	}

	public FileLoggerProvider(string directory, LogLevel minimumLevel = LogLevel.Information)
	{
		_directory = directory;
		_minimumLevel = minimumLevel;
		Directory.CreateDirectory(_directory);
	}

	public static string DefaultDirectory => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"AIDemon2", "logs");

	/// <summary>Plik na dobę — wystarczająca rotacja dla aplikacji desktopowej.</summary>
	private string CurrentFile => Path.Combine(_directory, $"aidemon2-{DateTime.Now:yyyyMMdd}.log");

	public ILogger CreateLogger(string categoryName) =>
		_loggers.GetOrAdd(categoryName, name => new FileLogger(this, name));

	private void Write(LogLevel level, string category, string message, Exception? exception)
	{
		var builder = new StringBuilder()
			.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
			.Append("  ")
			.Append(Skrot(level))
			.Append("  ")
			.Append(category)
			.Append("  ")
			.Append(message);

		if (exception is not null)
			builder.AppendLine().Append(exception);

		try
		{
			// Prosty zamek wystarcza: aplikacja jednoprocesowa, a logowanie nie leży
			// na ścieżce krytycznej wydajnościowo.
			lock (_writeGate)
				File.AppendAllText(CurrentFile, builder.AppendLine().ToString(), Encoding.UTF8);
		}
		catch (Exception)
		{
			// Logowanie nie może wywrócić aplikacji. Brak miejsca na dysku albo
			// zablokowany plik nie jest powodem, żeby użytkownik stracił sesję.
		}
	}

	private static string Skrot(LogLevel level) => level switch
	{
		LogLevel.Trace => "TRC",
		LogLevel.Debug => "DBG",
		LogLevel.Information => "INF",
		LogLevel.Warning => "OSTRZ",
		LogLevel.Error => "BLAD",
		LogLevel.Critical => "KRYT",
		_ => "???"
	};

	public void Dispose() => _loggers.Clear();

	private sealed class FileLogger : ILogger
	{
		private readonly FileLoggerProvider _provider;
		private readonly string _category;

		public FileLogger(FileLoggerProvider provider, string category)
		{
			_provider = provider;
			_category = category;
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) =>
			logLevel != LogLevel.None && logLevel >= _provider._minimumLevel;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
			Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
				return;

			_provider.Write(logLevel, _category, formatter(state, exception), exception);
		}
	}
}
