public interface IMessageRepository : IGenericRepository<Message>
{
	new Task<IEnumerable<Message>> GetAllAsync();
	Task<IEnumerable<Message>> GetAllFavouriteAsync();

	Task DeleteAllAsync();
}