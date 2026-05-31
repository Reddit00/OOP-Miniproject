using MyProject.Domain;

namespace MyProject.Infrastructure;

public class StorageZoneRepository : IRepository<StorageZone, Guid>
{
    private readonly IDataStore<StorageZone> _dataStore;
    private readonly List<StorageZone> _zones = new();

    public StorageZoneRepository(IDataStore<StorageZone> dataStore)
    {
        _dataStore = dataStore;
        
        var loadedZones = _dataStore.LoadAsync().GetAwaiter().GetResult();
        _zones.AddRange(loadedZones);
        if (!_zones.Any())
        {
            SeedInitialData();
        }
    }

    public IReadOnlyCollection<StorageZone> GetAll() => _zones.AsReadOnly();
    
    public StorageZone? GetById(Guid id) => _zones.FirstOrDefault(z => z.Id == id);
    
    public void Add(StorageZone entity) => _zones.Add(entity);
    
    public void Update(StorageZone entity)
    {
        var idx = _zones.FindIndex(z => z.Id == entity.Id);
        if (idx != -1)
        {
            _zones[idx] = entity;
        }
    }
    
    public void Delete(Guid id) => _zones.RemoveAll(z => z.Id == id);

    public async Task SaveAsync() => await _dataStore.SaveAsync(_zones);

    private void SeedInitialData()
    {
        _zones.Add(new StorageZone(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new ZoneAddress("A", 1, 1), 20.0));
        _zones.Add(new StorageZone(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), new ZoneAddress("B", 2, 4), 200.0));
        
        _dataStore.SaveAsync(_zones).GetAwaiter().GetResult();
    }
}