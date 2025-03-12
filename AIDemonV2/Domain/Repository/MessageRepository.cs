using Microsoft.EntityFrameworkCore;

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
	public MessageRepository(AIDemonDbContext context) : base(context)
	{
	}

	public async Task<IEnumerable<Message>> GetMessages()
	{
		return await _context.Messages.OrderBy(x => x.CreationDate).ToListAsync();
	}

	public async Task DeleteAllAsync()
	{
		var messages = await _context.Messages.ToListAsync();
		_context.Messages.RemoveRange(messages);
		await _context.SaveChangesAsync();
	}
}
