using AIDemon2.Extensions;
using Xunit;

namespace AIDemon2.Tests.Extensions;

/// <summary>
/// Ta metoda leży na ścieżce URUCHAMIANIA kodu (CodeRunnerService) i eksportu skryptu
/// do pliku. Cokolwiek tu przecieknie, trafia wprost do interpretera.
/// </summary>
public class StringExtensionsTests
{
	[Fact]
	public void RemoveMarkers_StripsFences_WhenBlockEndsWithNewline()
	{
		string wejscie = "```python\nprint(1)\n```";

		Assert.Equal("print(1)", wejscie.RemoveMarkdownCodeBlockMarkers());
	}

	[Fact]
	public void RemoveMarkers_StripsClosingFence_WhenNoTrailingNewline()
	{
		// Odpowiedzi modeli często nie mają znaku nowej linii przed zamykającym
		// płotkiem. Wtedy LastIndexOf('\n') nie ma czego znaleźć i backticki
		// zostawały w kodzie zapisywanym do pliku .py.
		string wejscie = "```python\nprint(1)```";

		Assert.Equal("print(1)", wejscie.RemoveMarkdownCodeBlockMarkers());
	}

	[Fact]
	public void RemoveMarkers_HandlesFenceWithoutLanguageTag()
	{
		string wejscie = "```\nprint(1)\n```";

		Assert.Equal("print(1)", wejscie.RemoveMarkdownCodeBlockMarkers());
	}

	[Fact]
	public void RemoveMarkers_LeavesPlainCodeUntouched()
	{
		string wejscie = "print(1)\nprint(2)";

		Assert.Equal(wejscie, wejscie.RemoveMarkdownCodeBlockMarkers());
	}

	[Fact]
	public void RemoveMarkers_HandlesSingleLineBlock()
	{
		// Blok bez znaku nowej linii w ogóle — cała odpowiedź w jednej linii.
		Assert.Equal("print(1)", "```print(1)```".RemoveMarkdownCodeBlockMarkers());
	}

	[Fact]
	public void RemoveMarkers_HandlesEmptyInput()
	{
		Assert.Equal(string.Empty, string.Empty.RemoveMarkdownCodeBlockMarkers());
	}

	[Fact]
	public void RemoveMarkers_DoesNotStripBackticksInsideCode()
	{
		// Backtick w środku kodu (np. w stringu) nie jest płotkiem markdownu.
		string wejscie = "print(\"``` nie usuwaj\")";

		Assert.Equal(wejscie, wejscie.RemoveMarkdownCodeBlockMarkers());
	}
}
