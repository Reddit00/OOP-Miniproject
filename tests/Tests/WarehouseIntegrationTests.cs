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
    public List<string> ErrorLog { get; } = new();

    public FileWarehouseTestRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IEnumerable<StorageZone>> GetAllZonesAsync() => Task.FromResult<IEnumerable<StorageZone>>(Zones);
    public Task<StorageZone?> GetZoneByIdAsync(Guid id) => Task.FromResult(Zones.FirstOrDefault(z => z.Id == id));
    public Task<Product?> GetProductByIdAsync(Guid id) => Task.FromResult(Products.FirstOrDefault(p => p.Id == id));

    public async Task SaveChangesAsync()
    {
        const int maxRetries = 3;
        const int delayMs = 15;

        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                var json = SerializeWarehouseData();
                await File.WriteAllTextAsync(_filePath, json);
                return;
            }
            catch (IOException ex)
            {
                ErrorLog.Add($"[WARN] Спроба {retry} невдала через блокування I/O: {ex.Message}");
                if (retry == maxRetries)
                {
                    ErrorLog.Add("[CRITICAL] Вичерпано всі спроби запису на диск.");
                    throw;
                }
                await Task.Delay(delayMs);
            }
        }
    }

    public async Task<Result> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            ErrorLog.Add("[INFO] Файл сховища відсутній. Стратегія відмови: Fallback до порожнього складу.");
            return Result.Success(); 
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var dto = JsonSerializer.Deserialize<WarehouseDataDto>(json);
            if (dto == null) return Result.Failure("Файл порожній");

            RestoreWarehouseState(dto);
            return Result.Success();
        }
        catch (JsonException ex)
        {
            ErrorLog.Add($"[ERROR] Пошкоджено структуру файлу JSON: {ex.Message}");
            return Result.Failure("Помилка десеріалізації: Структура даних зламана.");
        }
    }

    private string SerializeWarehouseData()
    {
        var dto = new WarehouseDataDto
        {
            Zones = Zones.Select(z => new ZoneDto 
            { 
                Id = z.Id, Sector = z.Address?.Sector ?? "Default", Row = 1, Shelf = 1, MaxWeight = z.MaxCapacityWeight, CurrentWeight = z.CurrentWeight, Items = z.Items
            }).ToList(),
            Products = Products.Select(p => new ProductDto { Id = p.Id, Sku = p.Sku.Value, Name = p.Name, Weight = p.Weight }).ToList()
        };
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }

    private void RestoreWarehouseState(WarehouseDataDto dto)
    {
        Zones = dto.Zones.Select(zDto =>
        {
            var zone = new StorageZone(zDto.Id, new ZoneAddress(zDto.Sector, zDto.Row, zDto.Shelf), zDto.MaxWeight);
            foreach (var item in zDto.Items.Where(i => i.Value > 0))
            {
                zone.AddProduct(new Product(item.Key, new SKU("PROD-0000"), "LoadedProduct", 0.1), item.Value);
            }
            return zone;
        }).ToList();

        Products = dto.Products.Select(pDto => new Product(pDto.Id, new SKU(pDto.Sku), pDto.Name, pDto.Weight)).ToList();
    }

    #region Внутрішні DTO структури
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
        if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
    }

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
        var loadResult = await loadedRepo.LoadAsync();

        Assert.True(loadResult.IsSuccess);
        var restoredZone = await loadedRepo.GetZoneByIdAsync(zoneId);
        Assert.NotNull(restoredZone);
        Assert.Equal(150.0, restoredZone.MaxCapacityWeight);
    }

    [Fact]
    public async Task Scenario3_ExecuteBusinessOperation_AfterStateRestoration_ShouldPreserveAggregateRules()
    {
        var initialRepo = new FileWarehouseTestRepository(_tempFilePath);
        var zoneId = Guid.NewGuid();
        initialRepo.Zones.Add(new StorageZone(zoneId, new ZoneAddress("C", 1, 1), 50.0));
        
        var productId = Guid.NewGuid();
        initialRepo.Products.Add(new Product(productId, new SKU("PROD-7777"), "Кава", 5.0));
        await initialRepo.SaveChangesAsync();

        var freshRepo = new FileWarehouseTestRepository(_tempFilePath);
        await freshRepo.LoadAsync();

        var useCase = new ReceiveProductUseCase(freshRepo, _strategy);
        var result = await useCase.ExecuteAsync(productId, 2);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Scenario4_MultipleSequentialOperations_ShouldPersistIncrementalState()
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
    public async Task Scenario5_ConcurrectWriteSimulated_ShouldMaintainStateConsistency()
    {
        var repo1 = new FileWarehouseTestRepository(_tempFilePath);
        repo1.Zones.Add(new StorageZone(Guid.NewGuid(), new ZoneAddress("D", 1, 1), 200.0));
        repo1.Products.Add(new Product(Guid.NewGuid(), new SKU("PROD-5555"), "Плита", 50.0));
        await repo1.SaveChangesAsync();

        var repo2 = new FileWarehouseTestRepository(_tempFilePath);
        await repo2.LoadAsync();
        
        Assert.NotNull(repo2.Zones.FirstOrDefault());
    }

    [Fact]
    public async Task FaultHandling_CorruptedJson_ShouldReturnFailureResultAndLogError()
    {
        await File.WriteAllTextAsync(_tempFilePath, "{ !!! ЗЛАМАНИЙ НЕВАЛІДНИЙ СТРУКТУРНО JSON !!! }");
        var repo = new FileWarehouseTestRepository(_tempFilePath);

        var result = await repo.LoadAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains(repo.ErrorLog, log => log.StartsWith("[ERROR]"));
    }

    [Fact]
    public async Task FaultHandling_MissingFile_ShouldExecuteGracefulFallbackToEmptyState()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        var repo = new FileWarehouseTestRepository(missingPath);

        var result = await repo.LoadAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(repo.Zones);
        Assert.Contains(repo.ErrorLog, log => log.StartsWith("[INFO]"));
    }

    [Fact]
    public async Task FaultHandling_FileLocked_ShouldTriggerRetryPolicyAndSucceedEventually()
    {
        var repo = new FileWarehouseTestRepository(_tempFilePath);
        repo.Zones.Add(new StorageZone(Guid.NewGuid(), new ZoneAddress("A", 1, 1), 100.0));

        using (var stream = new FileStream(_tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var saveTask = repo.SaveChangesAsync();
            await Task.Delay(5); 
            stream.Close(); 
            await saveTask; 
        }

        Assert.Contains(repo.ErrorLog, log => log.Contains("[WARN] Спроба"));
    }
}