using System.Net;
using System.Text;

namespace AIDemon2.Tests.Infrastructure;

/// <summary>
/// Podstawia odpowiedź HTTP bez wychodzenia w sieć. Konieczne, bo klient OpenRoutera
/// rozmawia z API wprost przez HttpClient — bez tego każdy jego test wymagałby
/// prawdziwego klucza API i płatnego zapytania.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
	private readonly Func<HttpRequestMessage, HttpResponseMessage> _odpowiedz;

	public List<HttpRequestMessage> Zadania { get; } = new();
	public List<string> Tresci { get; } = new();

	public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> odpowiedz)
	{
		_odpowiedz = odpowiedz;
	}

	public static FakeHttpMessageHandler Zwraca(HttpStatusCode kod, string cialo) =>
		new(_ => new HttpResponseMessage(kod)
		{
			Content = new StringContent(cialo, Encoding.UTF8, "application/json")
		});

	public static FakeHttpMessageHandler Rzuca(Exception wyjatek) =>
		new(_ => throw wyjatek);

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request, CancellationToken cancellationToken)
	{
		Zadania.Add(request);
		// Treść trzeba odczytać teraz — po zwróceniu odpowiedzi strumień bywa już zamknięty.
		Tresci.Add(request.Content is null
			? string.Empty
			: await request.Content.ReadAsStringAsync(cancellationToken));

		return _odpowiedz(request);
	}

	/// <summary>HttpClient gotowy do wstrzyknięcia, z adresem bazowym OpenRoutera.</summary>
	public HttpClient Klient() =>
		new(this) { BaseAddress = new Uri(AIDemon2.Services.ChatService.OpenRouterChatClient.BaseAddress) };
}
