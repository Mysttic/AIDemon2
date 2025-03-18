namespace AIDemon2.Extensions
{
	public static class StringExtensions
	{
		public static string RemoveMarkdownCodeBlockMarkers(this string code)
		{
			// Usuń otwierający marker, jeśli istnieje
			if (code.StartsWith("```"))
			{
				int firstNewline = code.IndexOf('\n');
				if (firstNewline >= 0)
				{
					code = code.Substring(firstNewline + 1);
				}
			}
			// Usuń zamykający marker, jeśli istnieje
			if (code.EndsWith("```"))
			{
				int lastNewline = code.LastIndexOf('\n');
				if (lastNewline >= 0)
				{
					code = code.Substring(0, lastNewline);
				}
			}
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
