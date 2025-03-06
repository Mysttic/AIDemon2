using System.ComponentModel.DataAnnotations.Schema;

public class Settings : EntityBase, ISettings
{
	public string ApiKey { get; set; }
	public string InstructionPrompt { get; set; }

	[ForeignKey(nameof(SelectedAIModelId))]
	public int? SelectedAIModelId { get; set; }
	public AIModel? SelectedAIModel { get; set; }
	public string? ProgrammingLanguage { get; set; }

	public Settings()
	{
	}
}

