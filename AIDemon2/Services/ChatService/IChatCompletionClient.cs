using AIDemon2.Services.ChatService;

/// <summary>
/// Jedna operacja, której aplikacja faktycznie używa: wyślij rozmowę, odbierz treść.
///
/// Abstrakcja istnieje, żeby logika czatu dała się testować bez wychodzenia w sieć,
/// a dostawca modeli był wymienny — co się właśnie przydało przy przejściu z io.net
/// na OpenRoutera: zmienił się wyłącznie adapter.
/// </summary>
public interface IChatCompletionClient
{
	/// <summary>Zwraca surową treść odpowiedzi modelu, bez czyszczenia.</summary>
	/// <exception cref="ChatServiceException">
	/// Gdy usługa zwróciła błąd albo pustą odpowiedź. Wywołujący dostaje komunikat
	/// gotowy do pokazania użytkownikowi.
	/// </exception>
	Task<string> CompleteAsync(OpenRouterChatRequest request, CancellationToken cancellationToken = default);
}
