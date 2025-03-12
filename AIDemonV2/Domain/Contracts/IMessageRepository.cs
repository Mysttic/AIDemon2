public interface IMessageRepository : IGenericRepository<Message>
{
	Task<IEnumerable<Message>> GetMessages();

	Task DeleteAllAsync();
}