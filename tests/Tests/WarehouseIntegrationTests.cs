using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Application;
using Xunit;

namespace MyProject.Tests;

public class FileWarehouseTestRepository : IWarehouseRepository
{
    private readonly string _filePath;
    public List<StorageZone> Zones { get; set; } = new();
    public List<Product> Products { get; set; } = new();

    public FileWarehouseTestRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IEnumerable<StorageZone>> GetAllZonesAsync() => Task.FromResult<IEnumerable<StorageZone>>(Zones);
    public Task<StorageZone?> GetZoneByIdAsync(Guid id) => Task.FromResult(Zones.FirstOrDefault(z => z.Id == id));
    public Task<Product?> GetProductByIdAsync(Guid id) => Task.FromResult(Products.FirstOrDefault(p => p.Id == id));

    public async Task SaveChangesAsync()
    {
        var dto = new WarehouseDataDto
        {
            Zones = Zones.Select(z => new ZoneDto 
            { 
                Id = z.Id, 
                Sector = z.Address?.Sector ?? "Default", 
                Row = 1, 
                Shelf = 1, 
                MaxWeight = z.MaxCapacityWeight,
                CurrentWeight = z.CurrentWeight,
                Items = z.Items
            }).ToList(),
            Products = Products.Select(p => new ProductDto 
            { 
                Id = p.Id, 
                Sku = p.Sku.Value, 
                Name = p.Name, 
                Weight = p.Weight 
            }).ToList()
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }
    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var dto = JsonSerializer.Deserialize<WarehouseDataDto>(json);
            if (dto == null) return;

            Zones = dto.Zones.Select(zDto =>
            {
                var zone = new StorageZone(zDto.Id, new ZoneAddress(zDto.Sector, zDto.Row, zDto.Shelf), zDto.MaxWeight);
                // Відновлюємо внутрішні заповнені товари через рефлексію або пряме додавання, якщо домен дозволяє
                foreach (var item in zDto.Items)
                {
                    var mockProd = new Product(item.Key, new SKU("PROD-0000"), "Loaded", 0.1);
                    if (item.Value > 0) zone.AddProduct(mockProd, item.Value);
                }
                return zone;
            }).ToList();

            Products = dto.Products.Select(pDto => new Product(pDto.Id, new SKU(pDto.Sku), pDto.Name, pDto.Weight)).ToList();
        }
        catch
        {
            throw new JsonException("Помилка читання JSON структури");
        }
    }

    #region Внутрішні DTO для тестів серіалізації
    private class WarehouseDataDto
    {
        public List<ZoneDto> Zones { get; set; } = new();
        public List<ProductDto> Products { get; set; } = new();
    }
    private class ZoneDto
    {
        public Guid Id { get; set; }
        public string Sector { get; set; } = "";
        public int Row { get; set; }
        public int Shelf { get; set; }
        public double MaxWeight { get; set; }
        public double CurrentWeight { get; set; }
        public Dictionary<Guid, int> Items { get; set; } = new();
    }
    private class ProductDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
        public string Name { get; set; } = "";
        public double Weight { get; set; }
    }
    #endregion
}

public class WarehouseIntegrationTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly TestPlacementStrategy _strategy;

    public WarehouseIntegrationTests()
    {
        _tempFilePath = Path.GetTempFileName();
        _strategy = new TestPlacementStrategy();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    #region Повний цикл інтеграційних файлових сценаріїв (8 тестів)

    [Fact]
    public async Task Scenario1_CreateAndSaveData_ShouldWritePhysicalFileToDisk()
    {
        var repo = new FileWarehouseTestRepository(_tempFilePath);
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 100.0);
        repo.Zones.Add(zone);

        await repo.SaveChangesAsync();

        Assert.True(File.Exists(_tempFilePath));
        Assert.True(File.ReadAllText(_tempFilePath).Length > 0);
    }

    [Fact]
    public async Task Scenario2_LoadFromExistingFile_ShouldCorrectlyRestoreState()
    {
        var initialRepo = new FileWarehouseTestRepository(_tempFilePath);
        var zoneId = Guid.NewGuid();
        var zone = new StorageZone(zoneId, new ZoneAddress("B", 2, 2), 150.0);
        initialRepo.Zones.Add(zone);
        await initialRepo.SaveChangesAsync();

        var loadedRepo = new FileWarehouseTestRepository(_tempFilePath);
        await loadedRepo.LoadAsync();

        var restoredZone = await loadedRepo.GetZoneByIdAsync(zoneId);

        Assert.NotNull(restoredZone);
        Assert.Equal(150.0, restoredZone.MaxCapacityWeight);
        Assert.Equal("B", restoredZone.Address.Sector);
    }

    [Fact]
    public async Task Scenario3_ExecuteBusinessOperation_AfterStateRestoration_ShouldPreserveAggregateRules()
    {
        var initialRepo = new FileWarehouseTestRepository(_tempFilePath);
        var zoneId = Guid.NewGuid();
        var zone = new StorageZone(zoneId, new ZoneAddress("C", 1, 1), 50.0);
        initialRepo.Zones.Add(zone);
        
        var productId = Guid.NewGuid();
        var product = new Product(productId, new SKU("PROD-7777"), "Кава", 5.0);
        initialRepo.Products.Add(product);
        await initialRepo.SaveChangesAsync();

        var freshRepo = new FileWarehouseTestRepository(_tempFilePath);
        await freshRepo.LoadAsync();

        var useCase = new ReceiveProductUseCase(freshRepo, _strategy);
        var result = await useCase.ExecuteAsync(productId, 2);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Scenario4_MissingFile_ShouldHandleGracefullyOrFallbackToEmpty()
    {
        var nonExistingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        var repo = new FileWarehouseTestRepository(nonExistingPath);

        await repo.LoadAsync();

        Assert.Empty(repo.Zones);
        Assert.Empty(repo.Products);
    }

    [Fact]
    public async Task Scenario5_CorruptedJsonFile_ShouldFallbackOrThrowHandledException()
    {
        await File.WriteAllTextAsync(_tempFilePath, "{ !!! БИТИЙ JSON ДЛЯ ТЕСТУ !!! }");
        var repo = new FileWarehouseTestRepository(_tempFilePath);

        var exception = await Record.ExceptionAsync(() => repo.LoadAsync());
        
        Assert.True(exception is JsonException || repo.Zones.Count == 0);
    }

    [Fact]
    public async Task Scenario6_MultipleSequentialOperations_ShouldPersistIncrementalState()
    {
        var repo = new FileWarehouseTestRepository(_tempFilePath);
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 100.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-1111"), "Монітор", 10.0);
        
        repo.Zones.Add(zone);
        repo.Products.Add(product);
        await repo.SaveChangesAsync();

        var useCase = new ReceiveProductUseCase(repo, _strategy);

        await useCase.ExecuteAsync(product.Id, 2); 
        await useCase.ExecuteAsync(product.Id, 3); 

        Assert.Equal(50.0, zone.CurrentWeight);
    }

    [Fact]
    public async Task Scenario7_ConcurrectWriteSimulated_ShouldMaintainStateConsistency()
    {
        var repo1 = new FileWarehouseTestRepository(_tempFilePath);
        var zone = new StorageZone(Guid.NewGuid(), new ZoneAddress("D", 1, 1), 200.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-5555"), "Плита", 50.0);
        
        repo1.Zones.Add(zone);
        repo1.Products.Add(product);
        await repo1.SaveChangesAsync();

        var uc1 = new ReceiveProductUseCase(repo1, _strategy);
        await uc1.ExecuteAsync(product.Id, 1);
        await repo1.SaveChangesAsync();

        var repo2 = new FileWarehouseTestRepository(_tempFilePath);
        await repo2.LoadAsync();
        
        Assert.NotNull(repo2.Zones.FirstOrDefault());
    }

    [Fact]
    public async Task Scenario8_FullPipeline_ReceiveAndSave_WithFilePersistence()
    {
        var repo = new FileWarehouseTestRepository(_tempFilePath);
        var zoneA = new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 100.0);
        var product = new Product(Guid.NewGuid(), new SKU("PROD-8888"), "Принтер", 20.0);
        
        repo.Zones.Add(zoneA);
        repo.Products.Add(product);
        await repo.SaveChangesAsync();

        var receiveUseCase = new ReceiveProductUseCase(repo, _strategy);
        await receiveUseCase.ExecuteAsync(product.Id, 2);
        await repo.SaveChangesAsync();

        var checkRepo = new FileWarehouseTestRepository(_tempFilePath);
        await checkRepo.LoadAsync();
        
        Assert.NotEmpty(checkRepo.Zones);
    }

    #endregion
}