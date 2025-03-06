using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class AIModelRepository : GenericRepository<AIModel>, IAIModelRepository
{
	public AIModelRepository(AIDemonDbContext context) : base(context)
	{
	}

	public async Task<AIModel?> GetByName(string? name)
	{
		var result = await _context.AIModels.FirstOrDefaultAsync(x => x.Name == name);
		return result ?? null;
	}
}
