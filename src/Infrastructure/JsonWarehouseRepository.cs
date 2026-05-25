using System.Text.Json;
using MyProject.Domain;

namespace MyProject.Infrastructure;

public class JsonWarehouseRepository : IWarehouseRepository
{
    private readonly string _zonesFilePath;
    private readonly List<StorageZone> _zones = new();

    public JsonWarehouseRepository(string zonesFilePath)
    {
        _zonesFilePath = zonesFilePath;
        LoadData();
    }

    public Task<StorageZone?> GetZoneByIdAsync(Guid id)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == id);
        return Task.FromResult(zone);
    }

    public Task UpdateStockAsync(Guid productId, Guid zoneId, int quantity)
    {
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(_zones, options);
        await File.WriteAllTextAsync(_zonesFilePath, jsonString);
    }

    private void LoadData()
    {
        if (!File.Exists(_zonesFilePath))
            {
                SeedInitialData();
                return;
            }

            try
            {
                string jsonString = File.ReadAllText(_zonesFilePath);
                var deserialized = JsonSerializer.Deserialize<List<StorageZone>>(jsonString);
                if (deserialized != null)
                {
                    _zones.Clear();
                    _zones.AddRange(deserialized);
                }
            }
            catch
            {
                SeedInitialData();
            }
    }

    private void SeedInitialData()
    {
        _zones.Clear();
        _zones.Add(new StorageZone(
            Guid.Parse("11111111-1111-1111-1111-111111111111"), 
            new ZoneAddress("A", 1, 1), 
            100.0 
        ));
        _zones.Add(new StorageZone(
            Guid.Parse("22222222-2222-2222-2222-222222222222"), 
            new ZoneAddress("B", 2, 3), 
            50.0
        ));
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(_zones, options);
        File.WriteAllText(_zonesFilePath, jsonString);
    }
}