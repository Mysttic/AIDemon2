using IoIntelligence.Client.Models.AIModel.Chat;

namespace AIDemon2.Tests.Infrastructure;

/// <summary>Atrapa klienta AI — zapamiętuje żądania i oddaje zaplanowaną odpowiedź.</summary>
public sealed class FakeChatCompletionClient : IChatCompletionClient
{
	private readonly Func<ChatCompletionRequest, string> _odpowiedz;

	public List<ChatCompletionRequest> Zadania { get; } = new();

	public FakeChatCompletionClient(string odpowiedz = "odpowiedz modelu")
		: this(_ => odpowiedz)
	{
	}

	public FakeChatCompletionClient(Func<ChatCompletionRequest, string> odpowiedz)
	{
		_odpowiedz = odpowiedz;
	}

	public static FakeChatCompletionClient Rzucajacy(Exception wyjatek) =>
		new(_ => throw wyjatek);

	public Task<string> CompleteAsync(ChatCompletionRequest request)
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
