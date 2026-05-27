namespace MyProject.Domain;

public class Product
{
    public Guid Id { get; }
    public SKU Sku { get; } 
    public string Name { get; private set; }
    public double Weight { get; }

    public Product(Guid id, SKU sku, string name, double weight)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID не може бути порожнім.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Назва товару не може бути порожньою.");
        if (weight <= 0) throw new ArgumentException("Вага товару повинна бути більшою за 0.");

        Id = id;
        Sku = sku;
        Name = name;
        Weight = weight;
    }
}