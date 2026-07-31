using Xunit;

namespace AIDemon2.Tests.Configs;

/// <summary>
/// Konfiguracja języków decyduje o tym, jaki proces i z jakimi argumentami zostanie
/// uruchomiony, więc jej błędy kończą się nieuruchomionym albo źle uruchomionym kodem.
/// </summary>
public class ProgrammingLanguageConfigTests
{
	[Fact]
	public void Extension_IsCaseInsensitive()
	{
		// Nazwa języka pochodzi z ustawień i z odpowiedzi modelu — wielkość liter bywa różna.
		Assert.Equal(".py", "python".ProgrammingLanguageExtension());
		Assert.Equal(".py", "Python".ProgrammingLanguageExtension());
		Assert.Equal(".py", "PYTHON".ProgrammingLanguageExtension());
	}

	[Fact]
	public void Extension_ReturnsEmpty_ForUnknownLanguage()
	{
		// CodeRunnerService opiera się na tym: puste rozszerzenie => NotSupportedException.
		Assert.Equal(string.Empty, "kobol".ProgrammingLanguageExtension());
	}

	[Fact]
	public void Interpreter_IsResolvedForKnownLanguages()
	{
		Assert.Equal("python", "python".ProgrammingLanguageInterpreter());
		Assert.Equal("cmd", "batch".ProgrammingLanguageInterpreter());
		Assert.Equal(string.Empty, "kobol".ProgrammingLanguageInterpreter());
	}

	[Fact]
	public void Arguments_FormatsPathIntoTemplate()
	{
		Assert.Equal("\"C:\\x.py\"", "python".ProgrammingLanguageArguments("C:\\x.py"));
		Assert.Equal("/C \"C:\\x.bat\"", "batch".ProgrammingLanguageArguments("C:\\x.bat"));
	}

	[Theory]
	[InlineData("python")]
	[InlineData("powershell")]
	[InlineData("batch")]
	[InlineData("nodejs")]
	[InlineData("bash")]
	public void Arguments_QuotePathWithSpaces(string jezyk)
	{
		// Katalog tymczasowy zawiera nazwę konta, więc spacja w ścieżce to codzienność
		// ("C:\Users\Jan Kowalski\AppData\Local\Temp\..."). Bez cudzysłowów interpreter
		// dostaje dwa argumenty zamiast jednej ścieżki i nie znajduje pliku.
		string sciezka = "C:\\Users\\Jan Kowalski\\Temp\\skrypt" + jezyk.ProgrammingLanguageExtension();

		string argumenty = jezyk.ProgrammingLanguageArguments(sciezka);

		Assert.Contains("\"" + sciezka + "\"", argumenty);
	}

	[Fact]
	public void Arguments_FallsBackToBarePath_ForUnknownLanguage()
	{
		Assert.Equal("C:\\x.txt", "kobol".ProgrammingLanguageArguments("C:\\x.txt"));
	}
}
