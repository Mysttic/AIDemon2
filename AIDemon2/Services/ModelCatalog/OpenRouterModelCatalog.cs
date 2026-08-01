using System.Text.Json;
using System.Text.Json.Serialization;
using AIDemon2.Domain;
using AIDemon2.Services.ChatService;
using Microsoft.Extensions.Logging;

namespace AIDemon2.Services.ModelCatalog;

/// <summary>
/// Pobiera listę modeli z OpenRoutera i trzyma kopię na dysku.
///
/// Poprzednio lista była wpisana na sztywno w zasobach aplikacji. Miało to dwie wady:
/// dezaktualizowała się przy każdej zmianie oferty dostawcy, a model zapisany w bazie,
/// lecz nieobecny na liście, wyświetlał się w ustawieniach jako PUSTE pole — i zapis
/// ustawień po cichu kasował wybór użytkownika.
///
/// Endpoint /models nie wymaga klucza API, więc listę da się pobrać także zanim
/// użytkownik cokolwiek skonfiguruje.
/// </summary>
public sealed class OpenRouterModelCatalog : IModelCatalog
{
	/// <summary>Używana, gdy nie ma ani sieci, ani kopii na dysku.</summary>
	internal static readonly IReadOnlyList<string> ListaAwaryjna = new[]
	{
		"anthropic/claude-sonnet-4.5",
		"deepseek/deepseek-r1",
		"google/gemini-2.5-flash",
		"meta-llama/llama-3.3-70b-instruct",
		"mistralai/mistral-large",
		"openai/gpt-4o",
		"openai/gpt-4o-mini",
		"qwen/qwen-2.5-coder-32b-instruct"
	};

	private readonly HttpClient _http;
	private readonly ILogger<OpenRouterModelCatalog> _logger;
	private readonly string _sciezkaKopii;
	private IReadOnlyList<string>? _wPamieci;

	public OpenRouterModelCatalog(HttpClient http, ILogger<OpenRouterModelCatalog> logger,
		string? sciezkaKopii = null)
	{
		_http = http;
		_logger = logger;
		_sciezkaKopii = sciezkaKopii ?? Path.Combine(DatabaseLocation.DataDirectory, "models-cache.json");
	}

	public OpenRouterModelCatalog(ILogger<OpenRouterModelCatalog> logger)
		: this(new HttpClient
		{
			BaseAddress = new Uri(OpenRouterChatClient.BaseAddress),
			// Domyślne 100 s trzymałoby okno ustawień zamknięte; jest kopia na dysku
			// i lista awaryjna, więc szybkie poddanie się jest tu lepsze niż czekanie.
			Timeout = TimeSpan.FromSeconds(10)
		}, logger)
	{
	}

	public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken cancellationToken = default)
	{
		if (_wPamieci is { Count: > 0 })
			return _wPamieci;

		var zSieci = await SprobujPobrac(cancellationToken);
		if (zSieci is { Count: > 0 })
		{
			_wPamieci = zSieci;
			ZapiszKopie(zSieci);
			return zSieci;
		}

		var zDysku = SprobujOdczytacKopie();
		if (zDysku is { Count: > 0 })
		{
			_logger.LogInformation("Model list loaded from the on-disk cache ({Count}).", zDysku.Count);
			_wPamieci = zDysku;
			return zDysku;
		}

		_logger.LogWarning("No network and no cached model list; falling back to the built-in list.");
		_wPamieci = ListaAwaryjna;
		return ListaAwaryjna;
	}

	private async Task<IReadOnlyList<string>?> SprobujPobrac(CancellationToken cancellationToken)
	{
		try
		{
			using var odpowiedz = await _http.GetAsync("models", cancellationToken);
			if (!odpowiedz.IsSuccessStatusCode)
			{
				_logger.LogWarning("Fetching the model list returned {StatusCode}.", (int)odpowiedz.StatusCode);
				return null;
			}

			await using var strumien = await odpowiedz.Content.ReadAsStreamAsync(cancellationToken);
			var wynik = await JsonSerializer.DeserializeAsync<OdpowiedzModeli>(
				strumien, OpenRouterJson.Options, cancellationToken);

			return Uporzadkuj(wynik?.Data?.Select(m => m.Id));
		}
		catch (Exception ex) when (ex is HttpRequestException or IOException
			or OperationCanceledException or JsonException or NotSupportedException)
		{
			// IOException łapie także HttpIOException z .NET 8 (zerwane połączenie
			// w trakcie czytania ciała), a OperationCanceledException — TaskCanceledException
			// z przekroczonego limitu czasu. Bez nich wyjątek uciekał z metody, która
			// w kontrakcie interfejsu deklaruje, że nigdy nie rzuca.
			_logger.LogWarning(ex, "Could not fetch the model list from OpenRouter.");
			return null;
		}
	}

	private static IReadOnlyList<string>? Uporzadkuj(IEnumerable<string?>? identyfikatory)
	{
		var lista = identyfikatory?
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Select(id => id!)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
			.ToList();

		return lista is { Count: > 0 } ? lista : null;
	}

	private void ZapiszKopie(IReadOnlyList<string> modele)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_sciezkaKopii)!);
			File.WriteAllText(_sciezkaKopii, JsonSerializer.Serialize(modele));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Kopia to wygoda, nie wymóg — brak zapisu nie może przerwać działania.
			_logger.LogWarning(ex, "Could not write the model list cache.");
		}
	}

	private IReadOnlyList<string>? SprobujOdczytacKopie()
	{
		try
		{
			if (!File.Exists(_sciezkaKopii))
				return null;
			return Uporzadkuj(JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_sciezkaKopii)));
		}
		catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
		{
			_logger.LogWarning(ex, "Could not read the model list cache.");
			return null;
		}
	}

	private sealed record OdpowiedzModeli
	{
		[JsonPropertyName("data")]
		public IReadOnlyList<PozycjaModelu>? Data { get; init; }
	}

	private sealed record PozycjaModelu
	{
		[JsonPropertyName("id")]
		public string? Id { get; init; }
	}
}
