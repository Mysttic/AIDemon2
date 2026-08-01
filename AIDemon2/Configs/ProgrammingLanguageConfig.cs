using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

/// <summary>
/// Konfiguracja uruchamiania skryptów, osobno dla Windows i Linuksa.
///
/// Poprzedni schemat miał jedno pole <c>launcher</c> wspólne dla wszystkich systemów
/// i nie potrafił wyrazić ani listy kandydatów, ani języka nieobsługiwanego na danym
/// systemie. To była przyczyna źródłowa większości niedziałających języków: "bash"
/// na Windows trafiał w launcher WSL zamiast w Git Bash, "groovy" nie był w ogóle
/// znajdowany (bo to plik .bat), a "python3" trafiał w zaślepkę Microsoft Store.
/// </summary>
public static class ProgrammingLanguageConfig
{
	public static readonly Dictionary<string, LanguageInfo> Languages;

	static ProgrammingLanguageConfig()
	{
		var assembly = Assembly.GetExecutingAssembly();
		string? resourceName = assembly
			.GetManifestResourceNames()
			.FirstOrDefault(name => name.EndsWith("ProgrammingLanguages.json", StringComparison.OrdinalIgnoreCase));

		if (resourceName == null)
			throw new FileNotFoundException("ProgrammingLanguages.json not found in embedded resources.");

		using Stream stream = assembly.GetManifestResourceStream(resourceName)
			?? throw new FileNotFoundException($"Nie udało się otworzyć zasobu {resourceName}.");
		using StreamReader reader = new StreamReader(stream);
		string json = reader.ReadToEnd();

		Languages = JsonSerializer.Deserialize<Dictionary<string, LanguageInfo>>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		}) ?? new Dictionary<string, LanguageInfo>();
	}

	private static LanguageInfo? Znajdz(string language) =>
		Languages.TryGetValue(language.ToLowerInvariant(), out var info) ? info : null;

	/// <summary>Ustawienia dla systemu, na którym aplikacja aktualnie działa.</summary>
	public static PlatformInfo? ForCurrentPlatform(this string language)
	{
		var info = Znajdz(language);
		if (info is null)
			return null;

		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? info.Windows : info.Linux;
	}

	public static string ProgrammingLanguageExtension(this string language) =>
		Znajdz(language)?.Extension ?? string.Empty;

	public static string ProgrammingLanguageArguments(this string language, string filePath)
	{
		var platforma = language.ForCurrentPlatform();
		return string.IsNullOrEmpty(platforma?.Arguments)
			? filePath
			: string.Format(platforma.Arguments, filePath);
	}

	/// <summary>
	/// Nazwy binarek do wypróbowania po kolei. Lista, nie pojedyncza nazwa, bo część
	/// interpreterów nie ma jednej pewnej nazwy (lua/lua5.4/luajit, pwsh/powershell).
	/// </summary>
	public static IReadOnlyList<string> ProgrammingLanguageLaunchers(this string language) =>
		language.ForCurrentPlatform()?.Launchers is { } lista
			? lista
			: Array.Empty<string>();

	/// <summary>Czy język da się w ogóle uruchomić na bieżącym systemie.</summary>
	public static bool IsSupportedOnThisPlatform(this string language)
	{
		var platforma = language.ForCurrentPlatform();
		if (platforma is null)
			return false;
		return platforma.Supported && (platforma.Launchers.Count > 0 || platforma.Shell is not null);
	}

	public static string UnsupportedReason(this string language) =>
		language.ForCurrentPlatform()?.UnsupportedReason
		?? $"Język '{language}' nie jest obsługiwany na tym systemie.";

	public class LanguageInfo
	{
		public string Extension { get; set; } = string.Empty;

		/// <summary>"lf" albo "crlf" — patrz <see cref="NormalizeLineEndings"/>.</summary>
		public string LineEndings { get; set; } = "lf";

		/// <summary>Doklejane na początek pliku, jeśli kodu tam nie ma (PHP i jego znacznik).</summary>
		public string? Preamble { get; set; }

		/// <summary>Obraz kontenera, w którym język da się sprawdzić na Linuksie.</summary>
		public string? Docker { get; set; }

		public PlatformInfo Windows { get; set; } = new();
		public PlatformInfo Linux { get; set; } = new();
	}

	public class PlatformInfo
	{
		public List<string> Launchers { get; set; } = new();
		public string Arguments { get; set; } = "\"{0}\"";
		public bool Supported { get; set; } = true;
		public string? UnsupportedReason { get; set; }

		/// <summary>
		/// "posix" oznacza, że interpretera nie ma pod stałą nazwą i trzeba go odnaleźć
		/// (Git Bash albo WSL) razem z przetłumaczeniem ścieżki skryptu.
		/// </summary>
		public string? Shell { get; set; }

		/// <summary>Ścieżka względem katalogu Git for Windows, gdy binarki nie ma na PATH.</summary>
		public string? GitBashFallback { get; set; }
	}

	/// <summary>
	/// Dopasowuje znaki końca linii do interpretera. Powłoki uniksowe przerywają
	/// z komunikatem o nieznanym poleceniu, gdy plik ma CRLF — a Windows domyślnie
	/// zapisuje właśnie CRLF.
	/// </summary>
	public static string NormalizeLineEndings(this string code, string language)
	{
		string lf = code.Replace("\r\n", "\n").Replace("\r", "\n");
		return Znajdz(language)?.LineEndings?.ToLowerInvariant() == "crlf"
			? lf.Replace("\n", "\r\n")
			: lf;
	}

	/// <summary>
	/// Dokleja wymagany nagłówek, jeśli kodu go nie ma. PHP bez znacznika
	/// <c>&lt;?php</c> nie wykonuje się — interpreter wypisuje źródło jako tekst
	/// i kończy z kodem 0, więc aplikacja uznałaby to za sukces.
	/// </summary>
	public static string ApplyPreamble(this string code, string language)
	{
		string? preambula = Znajdz(language)?.Preamble;
		if (string.IsNullOrEmpty(preambula))
			return code;

		// Sprawdzamy CAŁY kod, nie tylko początek: model potrafi poprzedzić znacznik
		// komentarzem albo blokiem HTML, a doklejenie drugiego <?php jest wtedy
		// błędem składni. Zdejmujemy też BOM, który psułby porównanie.
		string bezBom = code.TrimStart('﻿');
		if (bezBom.Contains("<?php", StringComparison.OrdinalIgnoreCase) || bezBom.Contains("<?="))
			return code;

		return preambula + code;
	}
}
