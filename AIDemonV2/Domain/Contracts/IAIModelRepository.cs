using System.Threading.Tasks;

public interface IAIModelRepository : IGenericRepository<AIModel>
{
	Task<AIModel?> GetByName(string? name);
}

