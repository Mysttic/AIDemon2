using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGenericRepository<T> where T : class
{
	Task<IEnumerable<T>> GetAll();
	Task<T?> GetById(int? id);
	Task<T> Add(T entity);
	Task<T> Update(T entity);
	Task<T> Delete(T entity);
}

