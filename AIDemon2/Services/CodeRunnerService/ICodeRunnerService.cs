public interface ICodeRunnerService
{
	/// <summary>
	/// Uruchamia kod zapisany w pliku tymczasowym i wywołuje callback z bieżącą linią wyjścia.
	/// Callback jest wołany z wątku puli, nie z wątku UI.
	/// </summary>
	/// <param name="cancellationToken">
	/// Anulowanie zabija proces wraz z całym drzewem potomnym. Niezależnie od niego
	/// obowiązuje limit czasu <see cref="CodeRunnerService.Timeout"/>.
	/// </param>
	Task RunCodeAsync(string code, string language, Action<string> onOutputReceived,
		CancellationToken cancellationToken = default);
}