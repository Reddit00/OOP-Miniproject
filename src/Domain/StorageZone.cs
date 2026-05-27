namespace MyProject.Domain;

public class StorageZone
{
    public Guid Id { get; }
    public ZoneAddress Address { get; }
    public double MaxWeightCapacity { get; }
    public double CurrentWeight { get; private set; }
    private readonly Dictionary<Guid, int> _items = new();
    public IReadOnlyDictionary<Guid, int> Items => _items;

    public StorageZone(Guid id, ZoneAddress address, double maxWeightCapacity)
    {
        if (maxWeightCapacity <= 0) throw new ArgumentException("Місткість ваги має бути більшою за 0.");
        
        Id = id;
        Address = address;
        MaxWeightCapacity = maxWeightCapacity;
        CurrentWeight = 0;
    }
    public void AddProduct(Product product, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Кількість має бути більшою за 0.");
        
        double addedWeight = product.Weight * quantity;
        if (CurrentWeight + addedWeight > MaxWeightCapacity)
        {
            throw new InvalidOperationException($"Перевищено ліміт ваги зони {Address}. " +
                $"Доступно: {MaxWeightCapacity - CurrentWeight} кг, спроба додати: {addedWeight} кг.");
        }

        if (_items.ContainsKey(product.Id))
            _items[product.Id] += quantity;
        else
            _items[product.Id] = quantity;

        CurrentWeight += addedWeight;
    }
}