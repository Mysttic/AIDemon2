namespace AIDemon2.Services.ChatService;

/// <summary>
/// Tłumaczy odpowiedź błędu OpenRoutera na zdanie, z którego użytkownik wie, co zrobić.
///
/// Wcześniej każde niepowodzenie — zły klucz, brak środków, przekroczony limit, padnięty
/// dostawca — kończyło się jednym komunikatem "sprawdź klucz API i połączenie z siecią".
/// Przy braku środków ta rada jest po prostu nieprawdziwa.
/// </summary>
public static class OpenRouterErrorText
{
	public static string Describe(int statusCode, string? apiMessage)
	{
		string szczegol = string.IsNullOrWhiteSpace(apiMessage) ? string.Empty : $" ({apiMessage})";

		return statusCode switch
		{
			400 => $"The request was rejected as invalid{szczegol}.",
			401 => "The API key was rejected. Check that it is correct and active in your OpenRouter dashboard.",
			402 => "Your OpenRouter account has no credit for this request. Top it up or pick a cheaper model.",
			403 => $"The content was blocked by the model's safety filters{szczegol}.",
			404 => "The selected model does not exist or is no longer available. Pick another one in the settings.",
			408 => "The model did not respond in time. Try again.",
			429 => "Rate limit exceeded. Wait a moment before trying again.",
			502 => "The provider returned an error for this model. Try again or pick another model.",
			503 => "No provider is currently available for this model. Pick another one.",
			_ => $"The AI service returned error {statusCode}{szczegol}."
		};
	}
}
