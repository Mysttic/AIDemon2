namespace AIDemon2.Extensions
{
	public static class StringExtensions
	{
		public static string RemoveMarkdownCodeBlockMarkers(this string code)
		{
			if (string.IsNullOrEmpty(code))
				return code;

			// Usuń otwierający płotek wraz z ewentualną nazwą języka ("```python").
			// Gdy po płotku nie ma znaku nowej linii, blok jest jednolinijkowy —
			// wtedy wystarczy zdjąć same trzy znaki.
			if (code.StartsWith("```"))
			{
				int firstNewline = code.IndexOf('\n');
				code = firstNewline >= 0
					? code.Substring(firstNewline + 1)
					: code.Substring(3);
			}

			// Usuń zamykający płotek. Poprzednia wersja szukała ostatniego znaku
			// nowej linii i przy jego braku (odpowiedź modelu bez końcowego newline'a)
			// zostawiała backticki w kodzie zapisywanym do pliku .py/.ps1 — czyli
			// wprost w treści przekazywanej interpreterowi.
			if (code.EndsWith("```"))
				code = code.Substring(0, code.Length - 3).TrimEnd('\r', '\n');

			return code;
		}

		//public static string ProgrammingLanguageLauncher(this string language)
		//{
		//	return language.ToLower() switch
		//	{
		//		"python" => "python",
		//		"powershell" => "pwsh",
		//		"batch" => "cmd",
		//		"nodejs" => "node",
		//		_ => string.Empty
		//	};
		//}

		//public static string ProgrammingLanguageExtension(this string language)
		//{
		//	return language.ToLower() switch
		//	{
		//		"python" => ".py",
		//		"powershell" => ".ps1",
		//		"batch" => ".bat",
		//		"nodejs" => ".js",
		//		_ => string.Empty
		//	};
		//}
	}
}
