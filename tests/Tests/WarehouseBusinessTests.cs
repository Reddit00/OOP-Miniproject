using MyProject.Domain;
using MyProject.Application;
using Xunit;

namespace MyProject.Tests;

public class WarehouseBusinessTests
{
    [Fact]
    public void AddProduct_WithinCapacity_ShouldIncreaseCurrentWeight()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 100.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1001"), "Ноутбук", 2.5);

        zone.AddProduct(product, 10); 

        Assert.Equal(25.0, zone.CurrentWeight);
        Assert.True(zone.Items.ContainsKey(product.Id));
        Assert.Equal(10, zone.Items[product.Id]);
    }

    [Fact]
    public void AddProduct_ExceedingCapacity_ShouldThrowInvalidOperationException()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 10.0); // Ліміт 10 кг
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1002"), "Мішок цементу", 25.0); // Вага 25 кг
        var exception = Assert.Throws<InvalidOperationException>(() => zone.AddProduct(product, 1));
        Assert.Contains("Перевищено ліміт ваги", exception.Message);
    }
}