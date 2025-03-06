public interface ISettings
{
	public string ApiKey { get; set; }
	public string InstructionPrompt { get; set; }
	public AIModel SelectedAIModel { get; set; }
	public string ProgrammingLanguage { get; set; }
}

