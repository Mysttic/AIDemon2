using System.Net;
using System.Text.Json;
using AIDemon2.Services.ChatService;
using AIDemon2.Tests.Infrastructure;
using Xunit;

namespace AIDemon2.Tests.Services;

/// <summary>
/// Kontrakt wobec OpenRoutera. Testy opisują przypadki, na których poprzedni klient
/// (io.net) się wykładał: pusta treść odpowiedzi i błąd przysłany z kodem 200.
/// </summary>
public class OpenRouterChatClientTests
{
	private static OpenRouterChatRequest Zadanie(string model = "openai/gpt-4o") => new()
	{
		Model = model,
		Messages = new[]
		{
			OpenRouterMessage.System("instrukcja"),
			OpenRouterMessage.User("pytanie")
		}
	};

	private static string OdpowiedzZTrescia(string tresc) =>
		JsonSerializer.Serialize(new
		{
			id = "gen-1",
			model = "openai/gpt-4o",
			choices = new[] { new { message = new { role = "assistant", content = tresc } } }
		});

	[Fact]
	public async Task Zwraca_Tresc_Pierwszego_Wyboru()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, OdpowiedzZTrescia("print(1)"));
		var klient = new OpenRouterChatClient(handler.Klient());

		var wynik = await klient.CompleteAsync(Zadanie());

		Assert.Equal("print(1)", wynik);
	}

	[Fact]
	public async Task Wysyla_Model_I_Role_W_Ciele_Zadania()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, OdpowiedzZTrescia("x"));
		var klient = new OpenRouterChatClient(handler.Klient());

		await klient.CompleteAsync(Zadanie("anthropic/claude-sonnet-4.5"));

		using var wyslane = JsonDocument.Parse(Assert.Single(handler.Tresci));
		Assert.Equal("anthropic/claude-sonnet-4.5", wyslane.RootElement.GetProperty("model").GetString());
		var wiadomosci = wyslane.RootElement.GetProperty("messages");
		Assert.Equal(2, wiadomosci.GetArrayLength());
		Assert.Equal("system", wiadomosci[0].GetProperty("role").GetString());
		Assert.Equal("user", wiadomosci[1].GetProperty("role").GetString());
	}

	[Fact]
	public async Task Uzywa_Wlasciwej_Sciezki_I_Metody()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, OdpowiedzZTrescia("x"));
		var klient = new OpenRouterChatClient(handler.Klient());

		await klient.CompleteAsync(Zadanie());

		var zadanie = Assert.Single(handler.Zadania);
		Assert.Equal(HttpMethod.Post, zadanie.Method);
		Assert.Equal("https://openrouter.ai/api/v1/chat/completions", zadanie.RequestUri!.ToString());
	}

	[Fact]
	public async Task Pomija_Pola_Opcjonalne_Gdy_Nieustawione()
	{
		// Wysyłanie "temperature": null zamiast pominięcia pola bywa odrzucane
		// przez walidację schematu po stronie dostawcy.
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, OdpowiedzZTrescia("x"));
		var klient = new OpenRouterChatClient(handler.Klient());

		await klient.CompleteAsync(Zadanie());

		using var wyslane = JsonDocument.Parse(Assert.Single(handler.Tresci));
		Assert.False(wyslane.RootElement.TryGetProperty("temperature", out _));
		Assert.False(wyslane.RootElement.TryGetProperty("max_tokens", out _));
	}

	[Theory]
	[InlineData(401, "API key")]
	[InlineData(402, "credit")]
	[InlineData(404, "model")]
	[InlineData(429, "Rate limit")]
	public async Task Tlumaczy_Kod_Bledu_Na_Czytelny_Komunikat(int kod, string oczekiwanyFragment)
	{
		// Wcześniej każdy z tych przypadków dawał jedno zdanie "sprawdź klucz API
		// i połączenie z siecią" — przy braku środków wprost mylące.
		var handler = FakeHttpMessageHandler.Zwraca((HttpStatusCode)kod,
			"""{"error":{"code":0,"message":"szczegoly od API"}}""");
		var klient = new OpenRouterChatClient(handler.Klient());

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => klient.CompleteAsync(Zadanie()));

		Assert.Contains(oczekiwanyFragment, wyjatek.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Wykrywa_Blad_Przyslany_Z_Kodem_200()
	{
		// OpenRouter potrafi oddać HTTP 200 z błędem w ciele. Poprzedni klient
		// robił Choices.First() i wywracał się na InvalidOperationException.
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK,
			"""{"error":{"code":429,"message":"rate limited"}}""");
		var klient = new OpenRouterChatClient(handler.Klient());

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => klient.CompleteAsync(Zadanie()));

		Assert.Contains("rate limit", wyjatek.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Pusta_Tresc_Daje_Czytelny_Wyjatek_Zamiast_NullReference()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK,
			"""{"id":"gen-1","choices":[{"message":{"role":"assistant","content":null}}]}""");
		var klient = new OpenRouterChatClient(handler.Klient());

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => klient.CompleteAsync(Zadanie()));

		Assert.Contains("empty reply", wyjatek.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Pusta_Tresc_Z_Powodem_Length_Nazywa_Przyczyne()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK,
			"""{"choices":[{"finish_reason":"length","message":{"content":null}}]}""");
		var klient = new OpenRouterChatClient(handler.Klient());

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => klient.CompleteAsync(Zadanie()));

		Assert.Contains("length limit", wyjatek.Message);
	}

	[Fact]
	public async Task Brak_Sieci_Daje_ChatServiceException()
	{
		var handler = FakeHttpMessageHandler.Rzuca(new HttpRequestException("brak sieci"));
		var klient = new OpenRouterChatClient(handler.Klient());

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => klient.CompleteAsync(Zadanie()));

		Assert.Contains("network", wyjatek.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Odpowiedz_Nie_Bedaca_JSON_em_Nie_Wysypuje_Aplikacji()
	{
		// Serwer proxy albo strona logowania sieci publicznej potrafi oddać HTML.
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, "<html>nie json</html>");
		var klient = new OpenRouterChatClient(handler.Klient());

		var wyjatek = await Assert.ThrowsAsync<ChatServiceException>(
			() => klient.CompleteAsync(Zadanie()));

		Assert.Contains("format", wyjatek.Message, StringComparison.OrdinalIgnoreCase);
	}
}
