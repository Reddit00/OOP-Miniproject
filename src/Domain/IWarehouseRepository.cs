namespace MyProject.Domain;

public interface IWarehouseRepository
{
    Task<StorageZone?> GetZoneByIdAsync(Guid id);
    Task<Product?> GetProductByIdAsync(Guid id);
    Task SaveChangesAsync();
    Task<IEnumerable<StorageZone>> GetAllZonesAsync();
}