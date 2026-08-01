using AIDemon2.Services.ChatService;

namespace AIDemon2.Tests.Infrastructure;

/// <summary>Atrapa klienta AI — zapamiętuje żądania i oddaje zaplanowaną odpowiedź.</summary>
public sealed class FakeChatCompletionClient : IChatCompletionClient
{
	private readonly Func<OpenRouterChatRequest, string> _odpowiedz;

	public List<OpenRouterChatRequest> Zadania { get; } = new();

	public FakeChatCompletionClient(string odpowiedz = "odpowiedz modelu")
		: this(_ => odpowiedz)
	{
	}

	public FakeChatCompletionClient(Func<OpenRouterChatRequest, string> odpowiedz)
	{
		_odpowiedz = odpowiedz;
	}

	public static FakeChatCompletionClient Rzucajacy(Exception wyjatek) =>
		new(_ => throw wyjatek);

	public Task<string> CompleteAsync(OpenRouterChatRequest request,
		CancellationToken cancellationToken = default)
	{
		Zadania.Add(request);
		return Task.FromResult(_odpowiedz(request));
	}
}

/// <summary>Liczy, ile razy fabryka utworzyła klienta i z jakim kluczem API.</summary>
public sealed class FakeClientFactory
{
	private readonly Func<string, IChatCompletionClient> _fabryka;

	public List<string> UzyteKlucze { get; } = new();
	public int LiczbaUtworzen => UzyteKlucze.Count;

	public FakeClientFactory(IChatCompletionClient klient)
		: this(_ => klient)
	{
	}

	public FakeClientFactory(Func<string, IChatCompletionClient> fabryka)
	{
		_fabryka = fabryka;
	}

	public Func<string, IChatCompletionClient> Delegat => klucz =>
	{
		UzyteKlucze.Add(klucz);
		return _fabryka(klucz);
	};
}
