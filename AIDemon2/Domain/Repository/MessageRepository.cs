using Microsoft.EntityFrameworkCore;

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
	public MessageRepository(AIDemonDbContext context) : base(context)
	{
	}

	public new async Task<IEnumerable<Message>> GetAllAsync()
	{
		return await _context.Messages.Where(m=>!m.Deleted).OrderBy(x => x.CreationDate).ToListAsync();
	}

	public async Task<IEnumerable<Message>> GetAllFavouriteAsync()
	{
		return await _context.Messages.Where(m => m.Favourite).OrderBy(x => x.CreationDate).ToListAsync();
	}

	public async Task DeleteAllAsync()
	{
		var messages = await _context.Messages.ToListAsync();
		if(messages.Any())
		{
			foreach (var message in messages)
				message.Deleted = true;
			await _context.SaveChangesAsync();
		}
	}
}