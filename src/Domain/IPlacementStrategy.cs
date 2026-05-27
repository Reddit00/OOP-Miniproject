namespace MyProject.Domain;

public interface IPlacementStrategy
{
    StorageZone? FindOptimalZone(Product product, int quantity, IEnumerable<StorageZone> zones);
}