namespace MyProject.Domain;

public abstract class WarehouseWorker
{
    public Guid Id { get; }
    public string Name { get; }

    protected WarehouseWorker(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ім'я не може бути порожнім.");
        Id = id;
        Name = name;
    }
    public abstract bool CanHandleWeight(double weight);
}