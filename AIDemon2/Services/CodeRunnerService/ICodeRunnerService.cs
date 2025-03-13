public interface ICodeRunnerService
{
	/// <summary>
	/// Uruchamia kod zapisany w pliku tymczasowym i wywołuje callback z bieżącą linią wyjścia.
	/// </summary>
	Task RunCodeAsync(string code, string language, Action<string> onOutputReceived);
}