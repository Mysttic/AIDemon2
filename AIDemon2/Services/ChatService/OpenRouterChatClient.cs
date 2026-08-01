using System.Net.Http.Json;
using System.Text.Json;

namespace AIDemon2.Services.ChatService;

/// <summary>
/// Klient HTTP OpenRoutera. Zastąpił pakiet io.net — OpenRouter wystawia jedno API
/// do wielu dostawców modeli, więc zmiana modelu nie wymaga zmiany kodu.
/// </summary>
public sealed class OpenRouterChatClient : IChatCompletionClient, IDisposable
{
	public const string BaseAddress = "https://openrouter.ai/api/v1/";

	private readonly HttpClient _http;
	private readonly bool _wlasnyHttpClient;

	public OpenRouterChatClient(string apiKey)
		: this(Utworz(apiKey), wlasnyHttpClient: true)
	{
	}

	/// <summary>Konstruktor dla testów: pozwala podstawić własny handler HTTP.</summary>
	public OpenRouterChatClient(HttpClient http, bool wlasnyHttpClient = false)
	{
		_http = http;
		_wlasnyHttpClient = wlasnyHttpClient;
	}

	private static HttpClient Utworz(string apiKey)
	{
		var http = new HttpClient { BaseAddress = new Uri(BaseAddress) };
		http.DefaultRequestHeaders.Authorization =
			new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
		// Opcjonalne nagłówki atrybucji — nie wpływają na działanie API,
		// pozwalają OpenRouterowi przypisać ruch do aplikacji.
		http.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/Mysttic/AIDemon2");
		http.DefaultRequestHeaders.Add("X-Title", "AIDemon2");
		// Model potrafi myśleć długo; limit i tak jest wyżej, po stronie anulowania z UI.
		http.Timeout = TimeSpan.FromMinutes(5);
		return http;
	}

	public async Task<string> CompleteAsync(OpenRouterChatRequest request,
		CancellationToken cancellationToken = default)
	{
		HttpResponseMessage odpowiedz;
		try
		{
			odpowiedz = await _http.PostAsJsonAsync(
				"chat/completions", request, OpenRouterJson.Options, cancellationToken);
		}
		catch (HttpRequestException ex)
		{
			throw new ChatServiceException(
				"Could not reach OpenRouter. Check your network connection.", ex);
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
		{
			throw new ChatServiceException("The AI service did not respond in time.", ex);
		}

		string tresc = await odpowiedz.Content.ReadAsStringAsync(cancellationToken);

		if (!odpowiedz.IsSuccessStatusCode)
			throw new ChatServiceException(
				OpenRouterErrorText.Describe((int)odpowiedz.StatusCode, WyluskajBlad(tresc)));

		OpenRouterChatResponse? wynik;
		try
		{
			wynik = JsonSerializer.Deserialize<OpenRouterChatResponse>(tresc, OpenRouterJson.Options);
		}
		catch (JsonException ex)
		{
			throw new ChatServiceException("The AI service returned an unexpected response format.", ex);
		}

		// Kod 200 nie gwarantuje powodzenia — błąd potrafi przyjść w ciele odpowiedzi.
		if (wynik?.Error is { } blad)
			throw new ChatServiceException(OpenRouterErrorText.Describe(blad.Code, blad.Message));

		var wybor = wynik?.Choices?.FirstOrDefault();
		string? zawartosc = wybor?.Message?.Content;

		if (string.IsNullOrEmpty(zawartosc))
			throw new ChatServiceException(wybor?.FinishReason switch
			{
				// Puste content przy tych powodach ma konkretną przyczynę, którą warto nazwać.
				"length" => "The reply hit the length limit before any content was produced.",
				"content_filter" => "The reply was blocked by the model's content filter.",
				_ => "The model returned an empty reply."
			});

		return zawartosc;
	}

	private static string? WyluskajBlad(string cialo)
	{
		if (string.IsNullOrWhiteSpace(cialo))
			return null;
		try
		{
			return JsonSerializer
				.Deserialize<OpenRouterErrorEnvelope>(cialo, OpenRouterJson.Options)?.Error?.Message;
		}
		catch (JsonException)
		{
			// Bramka albo serwer proxy potrafi oddać HTML zamiast JSON-a.
			return null;
		}
	}

	public void Dispose()
	{
		if (_wlasnyHttpClient)
			_http.Dispose();
	}
}
