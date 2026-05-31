using MyProject.Domain;

namespace MyProject.Infrastructure;

public class ProductRepository : IRepository<Product, Guid>
{
    private readonly IDataStore<Product> _dataStore;
    private readonly List<Product> _products = new();

    public ProductRepository(IDataStore<Product> dataStore)
    {
        _dataStore = dataStore;
        var loadedProducts = _dataStore.LoadAsync().GetAwaiter().GetResult();
        _products.AddRange(loadedProducts);

        if (!_products.Any())
        {
            SeedInitialData();
        }
    }

    public IReadOnlyCollection<Product> GetAll() => _products.AsReadOnly();
    public Product? GetById(Guid id) => _products.FirstOrDefault(p => p.Id == id);
    public void Add(Product entity) => _products.Add(entity);
    public void Update(Product entity) { }
    public void Delete(Guid id) => _products.RemoveAll(p => p.Id == id);

    private void SeedInitialData()
    {
        _products.Add(new Product(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new SKU("PROD-1001"), "Ігровий ноутбук", 2.5));
        _products.Add(new Product(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), new SKU("PROD-1002"), "Промисловий генератор", 15.0));
        
        _dataStore.SaveAsync(_products).GetAwaiter().GetResult();
    }
}