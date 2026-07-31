using Microsoft.EntityFrameworkCore;

public class SettingsRepository : GenericRepository<Settings>, ISettingsRepository
{
	public SettingsRepository(IDbContextFactory<AIDemonDbContext> contextFactory)
		: base(contextFactory)
	{
	}

	public async Task<Settings?> Get()
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		// OrderBy(Id): bez niego EF Core ostrzega w logu, że FirstOrDefault bez
		// sortowania może dać nieprzewidywalny wynik. Wiersz ustawień jest jeden,
		// ale zapytanie ma być deterministyczne także wtedy, gdy przestanie być.
		return await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
	}
}