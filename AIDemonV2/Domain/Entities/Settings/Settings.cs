using System.ComponentModel.DataAnnotations.Schema;

public class Settings : EntityBase, ISettings
{
	public string? ApiKey { get; set; }
	public string? InstructionPrompt { get; set; }
	public string? AIModel { get; set; }
	public string? ProgrammingLanguage { get; set; }
	public string ApiUrl => "https://aidemon.requestcatcher.com/";
	public Settings()
	{
	}
}

