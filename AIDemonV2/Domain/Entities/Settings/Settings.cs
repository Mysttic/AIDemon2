using System.ComponentModel.DataAnnotations.Schema;

public class Settings : EntityBase, ISettings
{
	public string ApiKey { get; set; }
	public string InstructionPrompt { get; set; }

	[ForeignKey(nameof(SelectedAIModelId))]
	public int? SelectedAIModelId { get; set; }
	public AIModel? SelectedAIModel { get; set; }

	[ForeignKey(nameof(ProgrammingLanguageId))]
	public int? ProgrammingLanguageId { get; set; }
	public ProgrammingLanguage? ProgrammingLanguage { get; set; }

	public Settings()
	{
	}
}

