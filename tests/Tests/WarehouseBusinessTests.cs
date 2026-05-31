using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MyProject.Domain;
using MyProject.Application;

namespace MyProject.Tests;

public class LocalFastAccessStrategy
{
    public StorageZone? FindZone(IEnumerable<StorageZone> zones, Product product, int quantity)
    {
        return zones
            .Where(z => z.MaxCapacityWeight - z.CurrentWeight >= product.Weight * quantity)
            .OrderByDescending(z => z.MaxCapacityWeight - z.CurrentWeight)
            .FirstOrDefault();
    }
}

public class WarehouseBusinessTests
{
    private class FakeWarehouseRepository : IWarehouseRepository
    {
        private readonly List<StorageZone> _zones = new();
        private readonly List<Product> _products = new();

        public FakeWarehouseRepository()
        {
            var zone1 = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 100.0);
            var zone2 = new StorageZone(Guid.NewGuid(), new ZoneAddress("B", 1, 1), 200.0);
            _zones.Add(zone1);
            _zones.Add(zone2);

            var product = new Product(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new SKU("PROD-1001"), "Тестовий Товар", 10.0);
            _products.Add(product);
        }

        public Task<IEnumerable<StorageZone>> GetAllZonesAsync() => Task.FromResult<IEnumerable<StorageZone>>(_zones);
        public Task<Product?> GetProductByIdAsync(Guid id) => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
        public Task<StorageZone?> GetZoneByIdAsync(Guid id) => Task.FromResult(_zones.FirstOrDefault(z => z.Id == id));
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    [Fact]
    public void Constructor_InvalidSkuFormat_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SKU("INVALID-SKU-123"));
    }

    [Fact]
    public void StorageZone_Constructor_NegativeCapacity_ShouldThrowArgumentException()
    {
        var address = new ZoneAddress("A", 1, 1);
        Assert.Throws<ArgumentException>(() => new StorageZone(Guid.NewGuid(), address, -5.0));
    }

    [Fact]
    public void Product_Constructor_ZeroWeight_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Product(Guid.NewGuid(), new SKU("PROD-1111"), "Товар", 0));
    }

    [Fact]
    public void AddProduct_ExceedingMaxCapacityWeight_ShouldThrowInvalidOperationException()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 10.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1001"), "Важкий Ноутбук", 6.0);
        Assert.Throws<InvalidOperationException>(() => zone.AddProduct(product, 2));
    }

    [Fact]
    public void RemoveProduct_ExceedingAvailableQuantity_ShouldThrowInvalidOperationException()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 50.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1002"), "Генератор", 15.0);
        zone.AddProduct(product, 1);
        var ex = Assert.Throws<InvalidOperationException>(() => zone.RemoveProduct(product, 2));
        Assert.Contains("Конфлікт залишків", ex.Message);
    }

    [Fact]
    public void RemoveProduct_ToZero_ShouldFullyRemoveKeyFromDictionary()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 50.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1002"), "Генератор", 15.0);
        zone.AddProduct(product, 2);
        zone.RemoveProduct(product, 2);
        Assert.False(zone.Items.ContainsKey(product.Id));
        Assert.Equal(0.0, zone.CurrentWeight);
    }

    [Fact]
    public void FastAccessStrategy_ShouldSelectZoneWithMostFreeSpace()
    {
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1001"), "Ноутбук", 2.5);
        var strategy = new LocalFastAccessStrategy(); 
        var zoneSmall = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 20.0);
        var zoneBig = new StorageZone(Guid.NewGuid(), new ZoneAddress("B", 1, 2), 100.0);
        var zones = new List<StorageZone> { zoneSmall, zoneBig };
        var selectedZone = strategy.FindZone(zones, product, 2);
        Assert.Equal(zoneBig.Id, selectedZone?.Id);
    }

    [Fact]
    public void StorageZone_AddProduct_ReachingNinetyPercent_ShouldTriggerObserverEvent()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 10.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1001"), "Ноутбук", 9.5);
        bool isObserverCalled = false;
        zone.OnCapacityWarning += (z, pct) =>
        {
            isObserverCalled = true;
            Assert.Equal(95.0, pct);
        };

        zone.AddProduct(product, 1);
        Assert.True(isObserverCalled);
    }

    [Fact]
    public async Task TransferProductUseCase_ValidScenario_ShouldSuccessfullyMoveProduct()
    {
        IWarehouseRepository repo = new FakeWarehouseRepository();
        var useCase = new TransferProductUseCase(repo);

        var zones = await repo.GetAllZonesAsync();
        var sourceZone = zones.First();
        var targetZone = zones.Last();
        var product = await repo.GetProductByIdAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        sourceZone.AddProduct(product!, 2);
        
        var result = await useCase.ExecuteAsync(sourceZone.Id, targetZone.Id, product!.Id, 1);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReceiveProductUseCase_NegativeQuantity_ShouldReturnFailureResult()
    {
        IWarehouseRepository repo = new FakeWarehouseRepository();
        var useCase = new ReceiveProductUseCase(repo);
        Guid prodId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var result = await useCase.ExecuteAsync(prodId, -5, "fast", (z, p) => { });
        Assert.False(result.IsSuccess);
        Assert.Contains("Кількість повинна бути > 0", result.ErrorMessage);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldExecuteSuccessfully()
    {
        IWarehouseRepository repo = new FakeWarehouseRepository();
        await repo.SaveChangesAsync();
        Assert.NotNull(repo);
    }

    [Fact]
    public async Task Repository_LoadData_FallbackToSeedData_ShouldBeNotEmpty()
    {
        IWarehouseRepository repo = new FakeWarehouseRepository();
        var zones = await repo.GetAllZonesAsync();
        Assert.NotEmpty(zones);
    }
}