using IoIntelligence.Client.Interfaces;
using IoIntelligence.Client.Models.AIModel.Chat;
using IoIntelligence.Client.Services;

/// <summary>
/// Cienka warstwa nad klientem io.net, sprowadzona do jednej operacji, której
/// aplikacja faktycznie używa.
///
/// Powód istnienia jest konkretny: <c>IIoIntelligenceClient.Models</c> zwraca klasę
/// <c>ModelClient</c>, a jej <c>CreateChatCompletionAsync</c> NIE jest wirtualna.
/// Samo wstrzyknięcie fabryki <c>IIoIntelligenceClient</c> nie pozwalało więc podstawić
/// odpowiedzi w teście — każdy test i tak wychodziłby w sieć. Ta abstrakcja zamyka
/// pakiet zewnętrzny w jednej klasie adaptera, a logikę czatu czyni testowalną.
/// </summary>
public interface IChatCompletionClient
{
	/// <summary>Zwraca surową treść odpowiedzi modelu, bez czyszczenia.</summary>
	Task<string> CompleteAsync(ChatCompletionRequest request);
}

/// <summary>Produkcyjna implementacja oparta o pakiet IONET.IOIntelligence.</summary>
public class IoIntelligenceChatClient : IChatCompletionClient
{
	private readonly IIoIntelligenceClient _client;

	public IoIntelligenceChatClient(string apiKey)
		: this(new IoIntelligenceClient(apiKey))
	{
	}

	public IoIntelligenceChatClient(IIoIntelligenceClient client)
	{
		_client = client;
	}

	public async Task<string> CompleteAsync(ChatCompletionRequest request)
	{
		var response = await _client.Models.CreateChatCompletionAsync(request);
		return response.Choices.First().Message.Content;
	}
}
