using Microsoft.EntityFrameworkCore;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
	protected readonly AIDemonDbContext _context;

	public GenericRepository(AIDemonDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<T>> GetAllAsync()
	{
		return await _context.Set<T>().ToListAsync();
	}

	public async Task<T?> GetByIdAsync(int? id)
	{
		var result = await _context.Set<T>().FindAsync(id);
		return result ?? null;
	}

	public async Task<T> AddAsync(T entity)
	{
		await _context.Set<T>().AddAsync(entity);
		await _context.SaveChangesAsync();
		return entity;
	}

	public async Task<T> UpdateAsync(T entity)
	{
		_context.Set<T>().Update(entity);
		await _context.SaveChangesAsync();
		return entity;
	}

	public async Task<T> DeleteAsync(T entity)
	{
		_context.Set<T>().Remove(entity);
		await _context.SaveChangesAsync();
		return entity;
	}
}