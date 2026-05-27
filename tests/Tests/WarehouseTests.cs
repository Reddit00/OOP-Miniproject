using MyProject.Domain;
using MyProject.Application;
using Xunit;

namespace MyProject.Tests;

public class WarehouseTests
{
    [Fact]
    public void AddProduct_ValidQuantity_ShouldIncreaseCurrentWeight()
    {
        var address = new ZoneAddress("A", 1, 1);
        var zone = new StorageZone(Guid.NewGuid(), address, 50.0); // Ліміт 50 кг
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1111"), "Монітор", 5.0); // 5 кг

        zone.AddProduct(product, 4); // 4 * 5кг = 20кг

        Assert.Equal(20.0, zone.CurrentWeight);
        Assert.Equal(4, zone.Items[product.Id]);
    }

    
    [Fact]
    public void AddProduct_ExceedingCapacity_ShouldThrowInvalidOperationException()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("B", 1, 1), 10.0); // Ліміт 10 кг
        var product = new Product(Guid.NewGuid(), new SKU("PROD-2222"), "Стіл", 15.0); // 15 кг

        var exception = Assert.Throws<InvalidOperationException>(() => zone.AddProduct(product, 1));
        Assert.Contains("Перевищено ліміт ваги", exception.Message);
    }

    
    [Fact]
    public void Constructor_InvalidSkuFormat_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SKU("invalid-sku-123"));
        Assert.Contains("SKU повинен відповідати формату", exception.Message);
    }

    [Fact]
    public async Task ReceiveProductUseCase_NegativeQuantity_ShouldThrowArgumentException()
    {
        var repository = new InMemoryWarehouseRepository();
        var useCase = new ReceiveProductUseCase(repository);
        Guid workerId = Guid.NewGuid();
        Guid productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid zoneId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            useCase.ExecuteAsync(workerId, productId, zoneId, -5));
        
        Assert.Contains("Кількість товару для оприбуткування повинна бути більшою за 0", exception.Message);
    }

    [Fact]
    public async Task ReceiveProductUseCase_NonExistingZone_ShouldThrowKeyNotFoundException()
    {
        var repository = new InMemoryWarehouseRepository();
        var useCase = new ReceiveProductUseCase(repository);
        Guid workerId = Guid.NewGuid();
        Guid productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid randomZoneId = Guid.NewGuid(); // Рандомний ID, якого немає в системі

        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            useCase.ExecuteAsync(workerId, productId, randomZoneId, 1));
    }
}