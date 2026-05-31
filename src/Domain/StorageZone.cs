using System;
using System.Collections.Generic;

namespace MyProject.Domain;

public delegate void CapacityWarningHandler(StorageZone zone, double occupancyPercentage);

public class StorageZone
{
    public Guid Id { get; private set; }
    public ZoneAddress Address { get; private set; }
    public double MaxCapacityWeight { get; private set; }
    public double CurrentWeight { get; private set; }
    public Dictionary<Guid, int> Items { get; private set; } = new();

    public event CapacityWarningHandler? OnCapacityWarning;

    public StorageZone(Guid id, ZoneAddress address, double maxCapacityWeight)
    {
        if (maxCapacityWeight <= 0) 
            throw new ArgumentException("Максимальна місткість повинна бути більшою за 0.");
        
        Id = id;
        Address = address;
        MaxCapacityWeight = maxCapacityWeight;
        CurrentWeight = 0;
    }

    public void AddProduct(Product product, int quantity)
    {
        if (quantity <= 0) 
            throw new ArgumentException("Кількість товару повинна бути більшою за 0.");
double addedWeight = product.Weight * quantity;
        if (CurrentWeight + addedWeight > MaxCapacityWeight) 
            throw new InvalidOperationException($"Недостатньо місця! Доступно: {MaxCapacityWeight - CurrentWeight} кг, потрібно: {addedWeight} кг.");

        if (Items.ContainsKey(product.Id))
            Items[product.Id] += quantity;
        else
            Items[product.Id] = quantity;

        CurrentWeight += addedWeight;

        double occupancy = (CurrentWeight / MaxCapacityWeight) * 100;
        if (occupancy >= 90.0)
        {
            OnCapacityWarning?.Invoke(this, occupancy);
        }
    }

    public void RemoveProduct(Product product, int quantity)
    {
        if (quantity <= 0) 
            throw new ArgumentException("Кількість товару для видалення повинна бути більшою за 0.");

        if (!Items.ContainsKey(product.Id) || Items[product.Id] < quantity) 
            throw new InvalidOperationException($"Конфлікт залишків: у комірці немає такої кількості товару {product.Name}.");

        Items[product.Id] -= quantity;
        CurrentWeight -= product.Weight * quantity;

        if (Items[product.Id] == 0)
        {
            Items.Remove(product.Id);
        }
    }
}