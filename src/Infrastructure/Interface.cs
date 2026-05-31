using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyProject.Application.Common;
public interface IRepository<T, TId>
{
    IReadOnlyCollection<T> GetAll();
    T? GetById(TId id);
    void Add(T entity);
    void Update(T entity);
    void Delete(TId id);
}

public interface IDataStore<T>
{
    Task<IReadOnlyCollection<T>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyCollection<T> items, CancellationToken cancellationToken = default);
}