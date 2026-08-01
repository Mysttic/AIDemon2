using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repozytorium tworzy własny <see cref="AIDemonDbContext"/> na każdą operację
/// i od razu go zwalnia.
///
/// Wcześniej kontekst był wstrzykiwany jako Scoped do serwisów zarejestrowanych
/// jako Singleton (captive dependency). Skutek: jeden kontekst na całe życie procesu,
/// nigdy niezwolniony — change tracker rósł w nieskończoność, dwie równoległe operacje
/// groziły wyjątkiem „A second operation was started on this context instance",
/// a encje zwracane z repozytoriów były współdzielonymi, śledzonymi obiektami
/// mutowanymi wprost z ViewModeli.
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
	protected readonly IDbContextFactory<AIDemonDbContext> _contextFactory;

	public GenericRepository(IDbContextFactory<AIDemonDbContext> contextFactory)
	{
		_contextFactory = contextFactory;
	}

	public virtual async Task<IEnumerable<T>> GetAllAsync()
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		return await context.Set<T>().ToListAsync();
	}

	public async Task<T?> GetByIdAsync(int? id)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		return await context.Set<T>().FindAsync(id);
	}

	public async Task<T> AddAsync(T entity)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		await context.Set<T>().AddAsync(entity);
		await context.SaveChangesAsync();
		return entity;
	}

	public async Task<T> UpdateAsync(T entity)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		context.Set<T>().Update(entity);
		await context.SaveChangesAsync();
		return entity;
	}

	public async Task<T> DeleteAsync(T entity)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		context.Set<T>().Remove(entity);
		await context.SaveChangesAsync();
		return entity;
	}
}
