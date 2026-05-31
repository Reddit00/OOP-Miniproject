using MyProject.Domain;

namespace MyProject.Application;

public class InMemoryWarehouseRepository : IWarehouseRepository
{
    private readonly List<StorageZone> _zones = new();
    private readonly List<Product> _products = new();

    public InMemoryWarehouseRepository()
    {
        SeedData();
    }

    public Task<StorageZone?> GetZoneByIdAsync(Guid id)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == id);
        return Task.FromResult(zone);
    }

    public Task<IEnumerable<StorageZone>> GetAllZonesAsync()
    {
    return Task.FromResult<IEnumerable<StorageZone>>(_zones); 
    }

    public Task<Product?> GetProductByIdAsync(Guid id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }

    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }

    private void SeedData()
    {
        _products.Add(new Product(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            new SKU("PROD-1001"),
            "Ігровий ноутбук",
            2.5
        ));

        _zones.Add(new StorageZone(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            new ZoneAddress("A", 1, 1),
            20.0 
        ));
    }
}