using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IMessageRepository : IGenericRepository<Message>
{
	Task<IEnumerable<Message>> GetMessages();
}

