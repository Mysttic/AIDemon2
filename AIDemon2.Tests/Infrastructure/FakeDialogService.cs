namespace AIDemon2.Tests.Infrastructure;

/// <summary>Atrapa dialogów — zapamiętuje pytania i oddaje zaplanowaną odpowiedź.</summary>
public sealed class FakeDialogService : IDialogService
{
	private readonly Queue<bool> _odpowiedzi = new();
	private readonly bool _domyslna;

	public List<string> ZadanePytania { get; } = new();
	public string? FormatEksportu { get; set; } = "csv";
	public string? SciezkaEksportu { get; set; } = @"C:\eksport.csv";

	public FakeDialogService(bool domyslnaOdpowiedz = true)
	{
		_domyslna = domyslnaOdpowiedz;
	}

	/// <summary>Kolejne wywołania dostaną kolejne odpowiedzi z tej listy.</summary>
	public FakeDialogService Zaplanuj(params bool[] odpowiedzi)
	{
		foreach (var o in odpowiedzi)
			_odpowiedzi.Enqueue(o);
		return this;
	}

	public void Initialize(Avalonia.Controls.Window mainWindow)
	{
	}

	public Task<bool> ShowConfirmationDialog(string title, string message, bool oneDecision = false)
	{
		ZadanePytania.Add(title);
		return Task.FromResult(_odpowiedzi.Count > 0 ? _odpowiedzi.Dequeue() : _domyslna);
	}

	public Task<string?> SelectExportFormat() => Task.FromResult(FormatEksportu);

	public Task<string?> SelectMessagesExportFilePath(string format) => Task.FromResult(SciezkaEksportu);

	public Task<string?> SelectMessageScriptExportFilePath(string language, string format) =>
		Task.FromResult(SciezkaEksportu);
}

/// <summary>Atrapa uruchamiania kodu — nic nie wykonuje, tylko notuje wywołania.</summary>
public sealed class FakeCodeRunnerService : ICodeRunnerService
{
	public List<(string Kod, string Jezyk)> Uruchomienia { get; } = new();
	public string Wyjscie { get; set; } = "wynik dzialania\n";

	/// <summary>Gdy ustawione, RunCodeAsync rzuca zamiast wypisywać wyjście.</summary>
	public Exception? Wyjatek { get; set; }

	public Task RunCodeAsync(string code, string language, Action<string> onOutputReceived,
		CancellationToken cancellationToken = default)
	{
		if (Wyjatek is not null)
			throw Wyjatek;

		Uruchomienia.Add((code, language));
		onOutputReceived(Wyjscie);
		return Task.CompletedTask;
	}
}

/// <summary>Atrapa eksportu — notuje, co i czy w ogóle zostało wyeksportowane.</summary>
public sealed class FakeMessageExportService : IMessageExportService
{
	public int LiczbaEksportowWszystkich { get; private set; }
	public List<Message> WyeksportowaneSkrypty { get; } = new();

	public Task ExportMessagesAsync()
	{
		LiczbaEksportowWszystkich++;
		return Task.CompletedTask;
	}

	public Task ExportMessageAsScriptAsync(Message message)
	{
		WyeksportowaneSkrypty.Add(message);
		return Task.CompletedTask;
	}
}
