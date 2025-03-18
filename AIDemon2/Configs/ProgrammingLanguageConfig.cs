using System.Reflection;
using System.Text.Json;

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

		using Stream stream = assembly.GetManifestResourceStream(resourceName);
		using StreamReader reader = new StreamReader(stream);
		string json = reader.ReadToEnd();

		Languages = JsonSerializer.Deserialize<Dictionary<string, LanguageInfo>>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		}) ?? new Dictionary<string, LanguageInfo>();
	}

	public static string ProgrammingLanguageInterpreter(this string language)
	{
		return Languages.TryGetValue(language.ToLower(), out var info) ? info.Launcher : string.Empty;
	}

	public static string ProgrammingLanguageExtension(this string language)
	{
		return Languages.TryGetValue(language.ToLower(), out var info) ? info.Extension : string.Empty;
	}

	public static string ProgrammingLanguageArguments(this string language, string filePath)
	{
		if (Languages.TryGetValue(language.ToLower(), out var info))
		{
			return string.IsNullOrEmpty(info.Arguments) ? filePath : string.Format(info.Arguments, filePath);
		}
		return filePath;
	}

	public class LanguageInfo
	{
		public string Launcher { get; set; }
		public string Extension { get; set; }
		public string Arguments { get; set; } = "";
	}
}
