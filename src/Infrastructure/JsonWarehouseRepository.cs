using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Application;

namespace MyProject.Infrastructure;

public class JsonWarehouseRepository : IWarehouseRepository
{
    private readonly string _filePath = "warehouse_storage.json";
    private List<StorageZone> _zones = new();
    private List<Product> _products = new();

    public JsonWarehouseRepository()
    {
        LoadDataWithRetry();
    }

    public Task<StorageZone?> GetZoneByIdAsync(Guid id)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == id);
        return Task.FromResult(zone);
    }

    public Task<Product?> GetProductByIdAsync(Guid id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }

    public Task<IEnumerable<StorageZone>> GetAllZonesAsync()
    {
        return Task.FromResult(_zones.AsEnumerable());
    }

    public async Task SaveChangesAsync()
    {
        int maxRetries = 3;
        int delayMs = 200;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var payload = new WarehousePayload { Zones = _zones, Products = _products };
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(payload, options);
                await File.WriteAllTextAsync(_filePath, json);
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
               
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Критична помилка I/O при збереженні стану: {ex.Message}", ex);
            }
        }
    }

    private void LoadDataWithRetry()
    {
        if (!File.Exists(_filePath))
        {
            SeedInitialData();
            return;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            var payload = JsonSerializer.Deserialize<WarehousePayload>(json);
            
            if (payload != null)
            {
                _zones = payload.Zones ?? new();
                _products = payload.Products ?? new();
            }
        }
        catch (JsonException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ПОМИЛКА СХОВИЩА] Файл {_filePath} пошкоджено! Структура JSON порушена.");
            Console.WriteLine("Система автоматично відновлює чистий еталонний стан для безпечної роботи.");
            Console.ResetColor();
            Console.ReadKey();

            SeedInitialData();
        }
    }

    private void SeedInitialData()
    {
        _products.Add(new Product(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            new SKU("PROD-1001"),
            "Ігровий ноутбук",
            2.5
        ));

        _zones.Add(new StorageZone(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            new ZoneAddress("A", 1, 1),
            20.0  // Ліміт 20 кг
        ));

        var payload = new WarehousePayload { Zones = _zones, Products = _products };
        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private class WarehousePayload
    {
        public List<StorageZone> Zones { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }
}