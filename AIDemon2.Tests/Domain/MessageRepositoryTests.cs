using Microsoft.EntityFrameworkCore;
﻿using AIDemon2.Tests.Infrastructure;
using Xunit;

namespace AIDemon2.Tests.Domain;

/// <summary>
/// Repozytorium wiadomości realizuje kasowanie miękkie (flaga Deleted). Każde zapytanie,
/// które o niej zapomni, pokaże użytkownikowi wiadomości, które uznał za usunięte.
/// </summary>
public class MessageRepositoryTests : IDisposable
{
	private readonly SqliteDbFixture _fixture = new();
	private readonly MessageRepository _repository;

	public MessageRepositoryTests()
	{
		_repository = new MessageRepository(_fixture);
	}

	private Message Dodaj(string tresc, bool ulubiona = false, bool usunieta = false, int minutaUtworzenia = 0)
	{
		var wiadomosc = new Message
		{
			MessageContent = tresc,
			OriginalMessage = tresc,
			Favourite = ulubiona,
			Deleted = usunieta,
			CreationDate = new DateTime(2026, 1, 1, 0, minutaUtworzenia, 0, DateTimeKind.Utc),
			ModificationDate = new DateTime(2026, 1, 1, 0, minutaUtworzenia, 0, DateTimeKind.Utc)
		};
		_fixture.Context.Messages.Add(wiadomosc);
		_fixture.Context.SaveChanges();
		return wiadomosc;
	}

	[Fact]
	public async Task GetAllFavourite_ExcludesSoftDeleted()
	{
		// Wiadomość usunięta, ale wcześniej oznaczona jako ulubiona, wracała na listę
		// ulubionych — filtr sprawdzał wyłącznie flagę Favourite.
		Dodaj("zostaje", ulubiona: true);
		Dodaj("usunieta", ulubiona: true, usunieta: true);
		_fixture.Detach();

		var wynik = await _repository.GetAllFavouriteAsync();

		Assert.Equal(new[] { "zostaje" }, wynik.Select(m => m.MessageContent));
	}

	[Fact]
	public async Task GetAll_ExcludesDeleted()
	{
		Dodaj("widoczna");
		Dodaj("skasowana", usunieta: true);
		_fixture.Detach();

		var wynik = await _repository.GetAllAsync();

		Assert.Equal(new[] { "widoczna" }, wynik.Select(m => m.MessageContent));
	}

	[Fact]
	public async Task GetAll_OrdersByCreationDate()
	{
		Dodaj("trzecia", minutaUtworzenia: 30);
		Dodaj("pierwsza", minutaUtworzenia: 10);
		Dodaj("druga", minutaUtworzenia: 20);
		_fixture.Detach();

		var wynik = await _repository.GetAllAsync();

		Assert.Equal(new[] { "pierwsza", "druga", "trzecia" }, wynik.Select(m => m.MessageContent));
	}

	[Fact]
	public async Task DeleteAll_SetsDeletedFlag_WithoutRemovingRows()
	{
		Dodaj("a");
		Dodaj("b");

		await _repository.DeleteAllAsync();
		_fixture.Detach();

		Assert.Empty(await _repository.GetAllAsync());
		// Wiersze mają zostać w bazie — to kasowanie miękkie, nie fizyczne.
		// IgnoreQueryFilters, bo globalny filtr modelu ukrywa skasowane wiersze.
		Assert.Equal(2, _fixture.Context.Messages.IgnoreQueryFilters().Count());
	}

	[Fact]
	public async Task GetAllFavourite_OrdersByCreationDate()
	{
		Dodaj("druga", ulubiona: true, minutaUtworzenia: 20);
		Dodaj("pierwsza", ulubiona: true, minutaUtworzenia: 10);
		_fixture.Detach();

		var wynik = await _repository.GetAllFavouriteAsync();

		Assert.Equal(new[] { "pierwsza", "druga" }, wynik.Select(m => m.MessageContent));
	}

	public void Dispose() => _fixture.Dispose();
}
