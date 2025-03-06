using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
	public MessageRepository(AIDemonDbContext context) : base(context)
	{
	}

	public async Task<IEnumerable<Message>> GetMessages()
	{
		return await _context.Messages.OrderBy(x => x.CreationDate).ToListAsync();
	}
}
