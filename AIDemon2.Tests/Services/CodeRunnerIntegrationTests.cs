using System.Runtime.InteropServices;
using System.Text;
using AIDemon2.Services.CodeRunnerService;
using Xunit;

namespace AIDemon2.Tests.Services;

/// <summary>
/// Uruchamia prawdziwe interpretery przez prawdziwy <see cref="CodeRunnerService"/>.
///
/// Testy jednostkowe sprawdzają konfigurację, ale nie odpowiadają na pytanie, które
/// naprawdę bolało: czy proces w ogóle wystartuje. To tutaj wychodzą rzeczy widoczne
/// dopiero na żywo — Git Bash spoza PATH, ścieżka wymagająca tłumaczenia, plik .bat,
/// którego CreateProcess nie znajduje.
///
/// Języki nieobecne na maszynie są pomijane, nie zgłaszane jako błąd: zestaw
/// interpreterów na maszynie dewelopera i na CI z natury się różni.
/// </summary>
public class CodeRunnerIntegrationTests
{
	private static bool NaWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

	private static async Task<string> Uruchom(string kod, string jezyk)
	{
		var bufor = new StringBuilder();
		var runner = new CodeRunnerService { Timeout = TimeSpan.FromSeconds(60) };

		await runner.RunCodeAsync(kod, jezyk, tekst =>
		{
			lock (bufor) bufor.Append(tekst);
		});

		lock (bufor) return bufor.ToString();
	}

	/// <summary>Czy da się w ogóle spróbować — inaczej test nie ma czego sprawdzać.</summary>
	private static bool Dostepny(string jezyk)
	{
		if (!jezyk.IsSupportedOnThisPlatform())
			return false;

		if (jezyk.ForCurrentPlatform()?.Shell == "posix")
			return PosixShellLocator.Locate() is not null;

		return jezyk.ProgrammingLanguageLaunchers().Any(ZnalezionoNaSciezce);
	}

	private static bool ZnalezionoNaSciezce(string nazwa)
	{
		string[] rozszerzenia = NaWindows && !Path.HasExtension(nazwa)
			? new[] { ".exe", ".bat", ".cmd" }
			: new[] { string.Empty };

		return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Any(katalog => rozszerzenia.Any(r =>
			{
				try
				{
					string p = Path.Combine(katalog.Trim('"'), nazwa + r);
					return File.Exists(p) && new FileInfo(p).Length > 0;
				}
				catch (ArgumentException) { return false; }
			}));
	}

	[Theory]
	[InlineData("python", "print(\"ZNACZNIK\")")]
	[InlineData("nodejs", "console.log(\"ZNACZNIK\")")]
	[InlineData("perl", "print \"ZNACZNIK\\n\";")]
	[InlineData("ruby", "puts \"ZNACZNIK\"")]
	[InlineData("lua", "print(\"ZNACZNIK\")")]
	[InlineData("groovy", "println \"ZNACZNIK\"")]
	[InlineData("go", "package main\nimport \"fmt\"\nfunc main() { fmt.Println(\"ZNACZNIK\") }")]
	public async Task Jezyk_Wypisuje_Na_Wyjscie(string jezyk, string kod)
	{
		if (!Dostepny(jezyk))
			return; // brak interpretera na tej maszynie — patrz uwaga w opisie klasy

		Assert.Contains("ZNACZNIK", await Uruchom(kod, jezyk));
	}

	[Fact]
	public async Task Powershell_Wypisuje_Na_Wyjscie()
	{
		if (!(NaWindows && Dostepny("powershell")))
			return; // powershell: tylko Windows

		Assert.Contains("ZNACZNIK", await Uruchom("Write-Output 'ZNACZNIK'", "powershell"));
	}

	[Fact]
	public async Task Batch_Wypisuje_Na_Wyjscie()
	{
		if (!(NaWindows && Dostepny("batch")))
			return; // batch: tylko Windows

		Assert.Contains("ZNACZNIK", await Uruchom("@echo ZNACZNIK", "batch"));
	}

	[Fact]
	public async Task Bash_Dziala_Mimo_Ze_Nie_Ma_Go_Na_Sciezce_Windows()
	{
		// Regresja: launcher "bash" na Windows trafiał w C:\Windows\System32\bash.exe
		// (launcher WSL), bo Git for Windows celowo nie dodaje swojego bin do PATH.
		// Skrypt nie uruchamiał się mimo zainstalowanego Git Basha.
		if (!(Dostepny("bash")))
			return; // bash: brak Git Basha i WSL

		Assert.Contains("ZNACZNIK", await Uruchom("echo ZNACZNIK", "bash"));
	}

	[Fact]
	public async Task Php_Dziala_Bez_Znacznika_Otwierajacego()
	{
		// Bez preambuły interpreter wypisuje źródło jako tekst i kończy z kodem 0 —
		// aplikacja uznałaby to za udane wykonanie.
		if (!(Dostepny("php")))
			return; // php: brak interpretera

		string wyjscie = await Uruchom("echo \"ZNACZNIK\\n\";", "php");

		Assert.Contains("ZNACZNIK", wyjscie);
		Assert.DoesNotContain("echo", wyjscie);
	}

	[Fact]
	public async Task Skrypt_Powloki_Dziala_Mimo_Konca_Linii_Windows()
	{
		// Model potrafi oddać kod z CRLF. Powłoki uniksowe przerywają wtedy
		// z "command not found" i niewidocznym \r na końcu polecenia.
		if (!(Dostepny("bash")))
			return; // bash: brak Git Basha i WSL

		Assert.Contains("ZNACZNIK", await Uruchom("echo ZNACZNIK\r\necho DRUGA\r\n", "bash"));
	}

	[Fact]
	public async Task Nieobslugiwany_Jezyk_Daje_Czytelny_Powod()
	{
		string jezyk = NaWindows ? "zsh" : "batch";

		var wyjatek = await Assert.ThrowsAsync<NotSupportedException>(
			() => Uruchom("echo x", jezyk));

		Assert.Equal(jezyk.UnsupportedReason(), wyjatek.Message);
	}


	[Fact]
	public async Task Brak_Interpretera_Nazywa_Czego_Brakuje()
	{
		// Wcześniej wychodził surowy komunikat Win32 "An error occurred trying to start
		// process 'groovy.bat'", z którego nie wynika, że po prostu brakuje interpretera.
		string jezyk = ProgrammingLanguageConfig.Languages.Keys
			.FirstOrDefault(j => j.IsSupportedOnThisPlatform() && !Dostepny(j))
			?? string.Empty;

		if (jezyk.Length == 0)
			return; // wszystkie obsługiwane języki są tu zainstalowane

		var wyjatek = await Assert.ThrowsAsync<NotSupportedException>(
			() => Uruchom("x", jezyk));

		Assert.Contains("No interpreter found", wyjatek.Message);
		Assert.Contains("PATH", wyjatek.Message);
	}

	[Fact]
	public async Task Nieznany_Jezyk_Jest_Odrzucany()
	{
		await Assert.ThrowsAsync<NotSupportedException>(() => Uruchom("x", "kobol"));
	}
}
