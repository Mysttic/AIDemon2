using System.Net;
using System.Text.Json;
using AIDemon2.Services.ModelCatalog;
using AIDemon2.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIDemon2.Tests.Services;

/// <summary>
/// Katalog modeli zastąpił listę wpisaną na sztywno w zasobach. Kluczowy wymóg:
/// okno ustawień musi dać się otworzyć także bez sieci, więc katalog nigdy nie rzuca
/// i nigdy nie zwraca pustej listy.
/// </summary>
public class OpenRouterModelCatalogTests : IDisposable
{
	private readonly string _kopia = Path.Combine(Path.GetTempPath(), $"modele-{Guid.NewGuid():N}.json");

	private static string OdpowiedzApi(params string[] identyfikatory) =>
		JsonSerializer.Serialize(new { data = identyfikatory.Select(id => new { id }).ToArray() });

	private OpenRouterModelCatalog Katalog(FakeHttpMessageHandler handler) =>
		new(handler.Klient(), NullLogger<OpenRouterModelCatalog>.Instance, _kopia);

	[Fact]
	public async Task Pobiera_Liste_Z_Api()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK,
			OdpowiedzApi("openai/gpt-4o", "anthropic/claude-sonnet-4.5"));

		var modele = await Katalog(handler).GetModelsAsync();

		Assert.Equal(new[] { "anthropic/claude-sonnet-4.5", "openai/gpt-4o" }, modele);
	}

	[Fact]
	public async Task Nie_Wymaga_Klucza_Api()
	{
		// Endpoint /models jest publiczny — lista musi dać się pobrać, zanim
		// użytkownik cokolwiek skonfiguruje.
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, OdpowiedzApi("openai/gpt-4o"));

		await Katalog(handler).GetModelsAsync();

		Assert.Null(Assert.Single(handler.Zadania).Headers.Authorization);
	}

	[Fact]
	public async Task Zapisuje_Kopie_I_Uzywa_Jej_Bez_Sieci()
	{
		var zSiecia = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, OdpowiedzApi("model/a", "model/b"));
		await Katalog(zSiecia).GetModelsAsync();
		Assert.True(File.Exists(_kopia));

		var bezSieci = FakeHttpMessageHandler.Rzuca(new HttpRequestException("brak sieci"));
		var modele = await Katalog(bezSieci).GetModelsAsync();

		Assert.Equal(new[] { "model/a", "model/b" }, modele);
	}

	[Fact]
	public async Task Bez_Sieci_I_Bez_Kopii_Oddaje_Liste_Awaryjna()
	{
		var handler = FakeHttpMessageHandler.Rzuca(new HttpRequestException("brak sieci"));

		var modele = await Katalog(handler).GetModelsAsync();

		Assert.Equal(OpenRouterModelCatalog.ListaAwaryjna, modele);
	}

	[Fact]
	public async Task Blad_Http_Nie_Wysypuje_Katalogu()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.InternalServerError, "{}");

		var modele = await Katalog(handler).GetModelsAsync();

		Assert.NotEmpty(modele);
	}

	[Fact]
	public async Task Uszkodzona_Odpowiedz_Nie_Wysypuje_Katalogu()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, "<html>nie json</html>");

		var modele = await Katalog(handler).GetModelsAsync();

		Assert.NotEmpty(modele);
	}

	[Fact]
	public async Task Pobiera_Tylko_Raz_Przy_Kolejnych_Wywolaniach()
	{
		var handler = FakeHttpMessageHandler.Zwraca(HttpStatusCode.OK, OdpowiedzApi("model/a"));
		var katalog = Katalog(handler);

		await katalog.GetModelsAsync();
		await katalog.GetModelsAsync();

		Assert.Single(handler.Zadania);
	}

	public void Dispose()
	{
		if (File.Exists(_kopia))
			File.Delete(_kopia);
	}
}
