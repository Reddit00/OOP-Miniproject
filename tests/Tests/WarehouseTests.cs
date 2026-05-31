using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        var zone = new StorageZone(Guid.NewGuid(), address, 50.0); 
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1111"), "Монітор", 5.0); 

        zone.AddProduct(product, 4);

        Assert.Equal(20.0, zone.CurrentWeight);
        Assert.Equal(4, zone.Items[product.Id]);
    }

   [Fact]
    public void AddProduct_ExceedingCapacity_ShouldThrowInvalidOperationException()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("B", 1, 1), 10.0); 
        var product = new Product(Guid.NewGuid(), new SKU("PROD-2222"), "Стіл", 15.0); 
        var exception = Assert.Throws<InvalidOperationException>(() => zone.AddProduct(product, 1));
        Assert.Contains("Недостатньо місця", exception.Message);
    }

    [Fact]
    public void Constructor_InvalidSkuFormat_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SKU("invalid-sku-123"));
        Assert.Contains("SKU повинен відповідати формату", exception.Message);
    }

    [Fact]
    public async Task ReceiveProductUseCase_NegativeQuantity_ShouldReturnFailureResult()
    {
        var repository = new InMemoryWarehouseRepository();
        var useCase = new ReceiveProductUseCase(repository);
        Guid productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var result = await useCase.ExecuteAsync(productId, -5, "fast", (zone, pct) => { });

        Assert.False(result.IsSuccess);
        Assert.Contains("Кількість повинна бути > 0", result.ErrorMessage);
    }

    [Fact]
    public async Task ReceiveProductUseCase_NonExistingProduct_ShouldReturnFailureResult()
    {
        var repository = new InMemoryWarehouseRepository();
        var useCase = new ReceiveProductUseCase(repository);
        Guid randomProductId = Guid.NewGuid(); 

        var result = await useCase.ExecuteAsync(randomProductId, 1, "fast", (zone, pct) => { });

        Assert.False(result.IsSuccess);
        Assert.Contains("Товар не знайдено", result.ErrorMessage);
    }

    [Fact]
    public void RemoveProduct_ExceedingAvailableQuantity_ShouldThrowInvalidOperationException()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 50.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-7777"), "Телевізор", 10.0);
        zone.AddProduct(product, 2); 
        var ex = Assert.Throws<InvalidOperationException>(() => zone.RemoveProduct(product, 3)); 
        Assert.Contains("Конфлікт залишків", ex.Message);
    }

    [Fact]
    public void RemoveProduct_ToZero_ShouldFullyRemoveKeyFromDictionary()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 50.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-8888"), "Чайник", 2.0);
        zone.AddProduct(product, 5);
        zone.RemoveProduct(product, 5);
        Assert.False(zone.Items.ContainsKey(product.Id)); 
        Assert.Equal(0.0, zone.CurrentWeight);
    }
}