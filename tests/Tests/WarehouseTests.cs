using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Application;
using Xunit;

namespace MyProject.Tests;

public class TestPlacementStrategy : IPlacementStrategy
{
    public StorageZone? FindZone(IEnumerable<StorageZone> zones, Product product, int quantity)
    {
        return zones
            .Where(z => z.MaxCapacityWeight - z.CurrentWeight >= product.Weight * quantity)
            .OrderByDescending(z => z.MaxCapacityWeight - z.CurrentWeight)
            .FirstOrDefault();
    }
}

public class AnalyticsFakeRepository : IWarehouseRepository
{
    public List<StorageZone> Zones { get; } = new();
    public List<Product> Products { get; } = new();

    public Task<IEnumerable<StorageZone>> GetAllZonesAsync() => Task.FromResult<IEnumerable<StorageZone>>(Zones);
    public Task<StorageZone?> GetZoneByIdAsync(Guid id) => Task.FromResult(Zones.FirstOrDefault(z => z.Id == id));
    public Task<Product?> GetProductByIdAsync(Guid id) => Task.FromResult(Products.FirstOrDefault(p => p.Id == id));
    public Task SaveChangesAsync() => Task.CompletedTask;
}

public class WarehouseTests
{
    #region 1. Інваріанти сутностей та валідація (Entity Invariants)

    [Fact]
    public void StorageZone_Constructor_NegativeCapacity_ShouldThrowArgumentException()
    {
        var address = new ZoneAddress("A", 1, 1);
        Assert.Throws<ArgumentException>(() => new StorageZone(Guid.NewGuid(), address, -10.0));
    }

    [Fact]
    public void Product_Constructor_ZeroOrNegativeWeight_ShouldThrowArgumentException()
    {
        var sku = new SKU("PROD-1001");
        Assert.Throws<ArgumentException>(() => new Product(Guid.NewGuid(), sku, "Товар", 0.0));
        Assert.Throws<ArgumentException>(() => new Product(Guid.NewGuid(), sku, "Товар", -5.5));
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("prod-1234")]
    [InlineData("PROD12345")]
    [InlineData("")]
    public void SKU_Constructor_InvalidFormat_ShouldThrowArgumentException(string invalidSku)
    {
        Assert.Throws<ArgumentException>(() => new SKU(invalidSku));
    }

    [Theory]
    [InlineData("PROD-1234")]
    [InlineData("ZONE-9999")] 
    [InlineData("TEST-0001")] 
    public void SKU_Constructor_ValidFormat_ShouldInitializeCorrectly(string validSku)
    {
        var sku = new SKU(validSku);
        Assert.Equal(validSku, sku.Value);
    }

    #endregion

    #region 2. Порушення лімітів та дублікати (Limits & Duplicates)

    [Fact]
    public void AddProduct_ExceedingCapacity_ShouldThrowInvalidOperationException()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("B", 1, 1), 10.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-2222"), "Важкий Стіл", 15.0);

        var exception = Assert.Throws<InvalidOperationException>(() => zone.AddProduct(product, 1));
        Assert.Contains("Недостатньо місця", exception.Message);
    }

    [Fact]
    public void AddProduct_ExactCapacityBoundary_ShouldSucceed()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 20.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-3333"), "Цегла", 10.0);

        zone.AddProduct(product, 2);

        Assert.Equal(20.0, zone.CurrentWeight);
    }

    [Fact]
    public void AddProduct_DuplicateProducts_ShouldIncrementQuantityCorrectly()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 50.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-4444"), "Кава", 5.0);

        zone.AddProduct(product, 2);
        zone.AddProduct(product, 3);

        Assert.Equal(25.0, zone.CurrentWeight);
        Assert.Equal(5, zone.Items[product.Id]);
    }

    #endregion

    #region 3. Видалення та залишкові стани (State Transitions & Edge Cases)

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
    public void RemoveProduct_NonExistingProduct_ShouldThrowInvalidOperationException()
    {
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 50.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1111"), "Товар А", 2.0);
        var nonExistingProduct = new Product(Guid.NewGuid(), new SKU("PROD-2222"), "Товар Б", 2.0);

        zone.AddProduct(product, 1);

        var ex = Assert.Throws<InvalidOperationException>(() => zone.RemoveProduct(nonExistingProduct, 1));
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

    #endregion

    #region 4. Патерн розширення та порожні колекції (Pattern Extension & Empty Collections)

    [Fact]
    public void PlacementStrategy_EmptyZonesList_ShouldReturnNull()
    {
        var strategy = new TestPlacementStrategy();
        var product = new Product(Guid.NewGuid(), new SKU("PROD-0000"), "Сканер", 1.0);
        var emptyZones = new List<StorageZone>();

        var result = strategy.FindZone(emptyZones, product, 1);

        Assert.Null(result);
    }

    [Fact]
    public void PlacementStrategy_NoZoneHasEnoughSpace_ShouldReturnNull()
    {
        var strategy = new TestPlacementStrategy();
        var product = new Product(Guid.NewGuid(), new SKU("PROD-5555"), "Тренажер", 60.0);

        var zone1 = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 50.0);
        var zone2 = new StorageZone(Guid.NewGuid(), new ZoneAddress("B", 1, 1), 40.0);
        var zones = new List<StorageZone> { zone1, zone2 };

        var result = strategy.FindZone(zones, product, 1);

        Assert.Null(result);
    }

    #endregion

    #region 5. Доменні сервіси та Аналітика (Domain Services & Analytics)

    [Fact]
    public async Task WarehouseAnalytics_EmptyRepository_ShouldReturnEmptyStatistics()
    {
        var repo = new AnalyticsFakeRepository();
        var service = new WarehouseAnalyticsService(repo);

        var stats = await service.GetWarehouseSummaryAsync();

        Assert.Equal(0.0, stats.TotalMaxCapacity);
        Assert.Equal(0.0, stats.TotalCurrentWeight);
        Assert.Equal(0.0, stats.GeneralOccupancyPercentage);
        Assert.Equal(0, stats.TotalUnitsStored);
    }

    [Fact]
    public async Task WarehouseAnalytics_FindProductLocations_ProductNotFound_ShouldReturnEmpty()
    {
        var repo = new AnalyticsFakeRepository();
        var service = new WarehouseAnalyticsService(repo);

        var locations = await service.FindProductLocationsAsync(Guid.NewGuid());

        Assert.Empty(locations);
    }

    #endregion

    #region 6. Сценарії Use Cases та обробка Result (Application Layer & Results)

    [Fact]
    public async Task ReceiveProductUseCase_NegativeQuantity_ShouldReturnFailureResult()
    {
        var repository = new AnalyticsFakeRepository();
        var strategy = new TestPlacementStrategy();
        var useCase = new ReceiveProductUseCase(repository, strategy);
        Guid productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await useCase.ExecuteAsync(productId, -5);

        Assert.False(result.IsSuccess);
        Assert.Contains("Кількість повинна бути > 0", result.ErrorMessage);
    }

    [Fact]
    public async Task ReceiveProductUseCase_NonExistingProduct_ShouldReturnFailureResult()
    {
        var repository = new AnalyticsFakeRepository();
        var strategy = new TestPlacementStrategy();
        var useCase = new ReceiveProductUseCase(repository, strategy);
        Guid randomProductId = Guid.NewGuid();

        var result = await useCase.ExecuteAsync(randomProductId, 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("Товар не знайдено", result.ErrorMessage);
    }

    #endregion
}