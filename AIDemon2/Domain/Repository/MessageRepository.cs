using Microsoft.EntityFrameworkCore;

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
	public MessageRepository(IDbContextFactory<AIDemonDbContext> contextFactory)
		: base(contextFactory)
	{
	}

	/// <summary>
	/// override, a nie "new": przy przesłanianiu metoda bazowa nadal byłaby wołana
	/// przez każdy kod trzymający referencję typu <c>GenericRepository&lt;Message&gt;</c>,
	/// i cicho zwracała także wiadomości skasowane.
	///
	/// Filtr !Deleted zapewnia globalny HasQueryFilter w OnModelCreating — tutaj
	/// zostaje wyłącznie sortowanie.
	/// </summary>
	public override async Task<IEnumerable<Message>> GetAllAsync()
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		return await context.Messages.OrderBy(x => x.CreationDate).ToListAsync();
	}

	public async Task<IEnumerable<Message>> GetAllFavouriteAsync()
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		return await context.Messages
			.Where(m => m.Favourite)
			.OrderBy(x => x.CreationDate)
			.ToListAsync();
	}

	public async Task DeleteAllAsync()
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		var messages = await context.Messages.ToListAsync();
		if (messages.Any())
		{
			foreach (var message in messages)
				message.Deleted = true;
			await context.SaveChangesAsync();
		}
	}
}
