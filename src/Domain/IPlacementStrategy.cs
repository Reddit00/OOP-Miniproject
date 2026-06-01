namespace MyProject.Domain;

public interface IPlacementStrategy
{
    StorageZone? FindZone(IEnumerable<StorageZone> zones, Product product, int quantity);
}