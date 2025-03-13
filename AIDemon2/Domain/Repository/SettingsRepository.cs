using Microsoft.EntityFrameworkCore;

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