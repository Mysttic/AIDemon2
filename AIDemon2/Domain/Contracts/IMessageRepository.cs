public interface IMessageRepository : IGenericRepository<Message>
{
	Task<IEnumerable<Message>> GetAllAsync();

	Task DeleteAllAsync();
}