using System.Runtime.InteropServices;
using Xunit;

namespace AIDemon2.Tests.Configs;

/// <summary>
/// Konfiguracja języków decyduje o tym, jaki proces i z jakimi argumentami zostanie
/// uruchomiony, więc jej błędy kończą się nieuruchomionym albo źle uruchomionym kodem.
/// </summary>
public class ProgrammingLanguageConfigTests
{
	private static bool NaWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

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
	public void Launchers_AreResolvedForKnownLanguages()
	{
		Assert.NotEmpty("python".ProgrammingLanguageLaunchers());
		Assert.Empty("kobol".ProgrammingLanguageLaunchers());
	}

	[Fact]
	public void Launchers_ArePlatformSpecific()
	{
		// Ta sama nazwa języka daje inny interpreter zależnie od systemu — schemat
		// z jednym wspólnym polem "launcher" nie potrafił tego wyrazić i to była
		// przyczyna źródłowa niedziałających języków.
		var python = "python".ProgrammingLanguageLaunchers();

		if (NaWindows)
			// "python3" na Windows trafia w zerobajtową zaślepkę Microsoft Store.
			Assert.Equal(new[] { "py", "python" }, python);
		else
			Assert.Equal(new[] { "python3", "python" }, python);
	}

	[Fact]
	public void Groovy_OnWindows_UsesExplicitBatExtension()
	{
		// Dystrybucja Groovy daje groovy.bat, a CreateProcess z UseShellExecute=false
		// nie korzysta z PATHEXT — dopisuje wyłącznie ".exe". Bez jawnego rozszerzenia
		// interpreter nie zostanie w ogóle odnaleziony.
		if (!NaWindows)
			return;

		Assert.Contains("groovy.bat", "groovy".ProgrammingLanguageLaunchers());
	}

	[Fact]
	public void Arguments_FormatsPathIntoTemplate()
	{
		Assert.Equal("\"C:\\x.py\"", "python".ProgrammingLanguageArguments("C:\\x.py"));
	}

	[Fact]
	public void PowerShell_UsesFileSwitch()
	{
		// Bez -File PowerShell traktuje argument jako polecenie do wykonania,
		// więc ścieżka ze spacją rozpadała się na osobne argumenty.
		if (!NaWindows)
			return;

		string argumenty = "powershell".ProgrammingLanguageArguments("C:\\a b\\s.ps1");

		Assert.Contains("-File", argumenty);
		Assert.Contains("\"C:\\a b\\s.ps1\"", argumenty);
	}

	[Theory]
	[InlineData("python")]
	[InlineData("nodejs")]
	[InlineData("ruby")]
	[InlineData("perl")]
	[InlineData("lua")]
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

	[Fact]
	public void UnsupportedLanguage_IsReportedWithReason()
	{
		// zsh na Windows i batch na Linuksie nie istnieją. Aplikacja ma to powiedzieć
		// wprost, zamiast wywracać się na Win32Exception przy starcie procesu.
		string jezyk = NaWindows ? "zsh" : "batch";

		Assert.False(jezyk.IsSupportedOnThisPlatform());
		Assert.NotEmpty(jezyk.UnsupportedReason());
		Assert.DoesNotContain("nie jest obsługiwany na tym systemie", jezyk.UnsupportedReason());
	}

	[Fact]
	public void SupportedLanguages_ReportSupport()
	{
		Assert.True("python".IsSupportedOnThisPlatform());
		Assert.True("bash".IsSupportedOnThisPlatform());
		Assert.False("kobol".IsSupportedOnThisPlatform());
	}

	[Fact]
	public void Php_GetsOpeningTag_WhenMissing()
	{
		// Bez znacznika interpreter wypisuje źródło jako tekst i kończy z kodem 0,
		// więc aplikacja uznałaby taki przebieg za udany.
		Assert.StartsWith("<?php", "echo \"x\";".ApplyPreamble("php"));
	}

	[Fact]
	public void Php_KeepsExistingOpeningTag()
	{
		string kod = "<?php echo \"x\";";

		Assert.Equal(kod, kod.ApplyPreamble("php"));
	}

	[Fact]
	public void Preamble_IsNotAddedToOtherLanguages()
	{
		Assert.Equal("print(1)", "print(1)".ApplyPreamble("python"));
	}

	[Fact]
	public void ShellLanguages_GetUnixLineEndings()
	{
		// CRLF w skrypcie powłoki daje "command not found" z niewidocznym \r
		// na końcu każdego polecenia.
		Assert.Equal("echo a\necho b", "echo a\r\necho b".NormalizeLineEndings("bash"));
	}

	[Fact]
	public void Batch_GetsWindowsLineEndings()
	{
		Assert.Equal("echo a\r\necho b", "echo a\necho b".NormalizeLineEndings("batch"));
	}

	[Fact]
	public void EveryLanguage_HasExtensionAndAtLeastOnePlatform()
	{
		foreach (var (nazwa, info) in ProgrammingLanguageConfig.Languages)
		{
			Assert.False(string.IsNullOrWhiteSpace(info.Extension), $"{nazwa}: brak rozszerzenia");

			bool gdziekolwiek =
				(info.Windows.Supported && (info.Windows.Launchers.Count > 0 || info.Windows.Shell is not null)) ||
				(info.Linux.Supported && (info.Linux.Launchers.Count > 0 || info.Linux.Shell is not null));

			Assert.True(gdziekolwiek, $"{nazwa}: nie da się uruchomić na żadnym systemie");
		}
	}

	[Fact]
	public void UnsupportedPlatforms_ExplainWhy()
	{
		foreach (var (nazwa, info) in ProgrammingLanguageConfig.Languages)
		{
			if (!info.Windows.Supported)
				Assert.False(string.IsNullOrWhiteSpace(info.Windows.UnsupportedReason),
					$"{nazwa}: brak powodu dla Windows");
			if (!info.Linux.Supported)
				Assert.False(string.IsNullOrWhiteSpace(info.Linux.UnsupportedReason),
					$"{nazwa}: brak powodu dla Linuksa");
		}
	}
}
