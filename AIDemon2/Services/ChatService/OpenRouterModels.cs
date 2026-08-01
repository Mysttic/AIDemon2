using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIDemon2.Services.ChatService;

/// <summary>
/// Odwzorowanie kontraktu HTTP OpenRoutera. Zastępuje typy z pakietu io.net, który
/// przeciekał poza adapter aż do logiki czatu i do testów.
///
/// Nie ma oficjalnego SDK OpenRoutera dla .NET (są tylko TypeScript, Python i Go),
/// więc rozmawiamy z API wprost przez HttpClient. Pakiet OpenAI dałoby się nagiąć
/// podmianą adresu, ale nie modeluje pól swoistych dla OpenRoutera i nie jest
/// przez niego testowany.
/// </summary>
public sealed record OpenRouterChatRequest
{
	[JsonPropertyName("model")]
	public required string Model { get; init; }

	[JsonPropertyName("messages")]
	public required IReadOnlyList<OpenRouterMessage> Messages { get; init; }

	[JsonPropertyName("temperature")]
	public double? Temperature { get; init; }

	[JsonPropertyName("max_tokens")]
	public int? MaxTokens { get; init; }
}

public sealed record OpenRouterMessage
{
	/// <summary>Dozwolone: system, user, assistant, tool, developer.</summary>
	[JsonPropertyName("role")]
	public required string Role { get; init; }

	[JsonPropertyName("content")]
	public required string Content { get; init; }

	public static OpenRouterMessage System(string content) => new() { Role = "system", Content = content };
	public static OpenRouterMessage User(string content) => new() { Role = "user", Content = content };
}

public sealed record OpenRouterChatResponse
{
	[JsonPropertyName("id")]
	public string? Id { get; init; }

	[JsonPropertyName("model")]
	public string? Model { get; init; }

	[JsonPropertyName("choices")]
	public IReadOnlyList<OpenRouterChoice>? Choices { get; init; }

	/// <summary>Odpowiedź z kodem 200 też potrafi nieść błąd w ciele.</summary>
	[JsonPropertyName("error")]
	public OpenRouterError? Error { get; init; }
}

public sealed record OpenRouterChoice
{
	[JsonPropertyName("message")]
	public OpenRouterResponseMessage? Message { get; init; }

	/// <summary>Znormalizowane: tool_calls, stop, length, content_filter, error, null.</summary>
	[JsonPropertyName("finish_reason")]
	public string? FinishReason { get; init; }
}

public sealed record OpenRouterResponseMessage
{
	[JsonPropertyName("role")]
	public string? Role { get; init; }

	/// <summary>Może być null — model bywa ucięty limitem albo odfiltrowany.</summary>
	[JsonPropertyName("content")]
	public string? Content { get; init; }
}

public sealed record OpenRouterError
{
	[JsonPropertyName("code")]
	public int Code { get; init; }

	[JsonPropertyName("message")]
	public string? Message { get; init; }
}

public sealed record OpenRouterErrorEnvelope
{
	[JsonPropertyName("error")]
	public OpenRouterError? Error { get; init; }
}

public static class OpenRouterJson
{
	public static readonly JsonSerializerOptions Options = new()
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		// Nazwy bierzemy wyłącznie z JsonPropertyName; żadnej polityki nazw,
		// bo API miesza konwencje (finish_reason obok id).
		PropertyNamingPolicy = null
	};
}
