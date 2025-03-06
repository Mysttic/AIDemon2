using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class SettingsRepository : GenericRepository<Settings>, ISettingsRepository
{
	public SettingsRepository(AIDemonDbContext context) : base(context)
	{
	}
	public async Task<Settings?> Get()
	{
		return await _context.Settings.FirstOrDefaultAsync();
	}	
}