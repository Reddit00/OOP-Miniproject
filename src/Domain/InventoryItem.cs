namespace MyProject.Domain;

public class InventoryItem
{
    public Guid ProductId { get; }
    public Guid ZoneId { get; }
    public int Quantity { get; private set; }

    public InventoryItem(Guid productId, Guid zoneId, int quantity)
    {
        if (quantity < 0) throw new ArgumentException("Кількість не може бути від'ємною.");
        ProductId = productId;
        ZoneId = zoneId;
        Quantity = quantity;
    }
}