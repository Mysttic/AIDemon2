public class SavedMessageRepository : GenericRepository<SavedMessage>, ISavedMessageRepository
{
	public SavedMessageRepository(AIDemonDbContext context) : base(context)
	{
	}
}
