public class AIModel : EntityBase, IAIModel
{
	public string Name { get; set; }
	public AIModel()
	{ }
	public override string ToString()
	{
		return Name;
	}
}

