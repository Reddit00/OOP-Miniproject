using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Domain;
using MyProject.Application.Common;

namespace MyProject.Infrastructure;

public class ZoneRepository : IRepository<StorageZone, Guid>
{
    private readonly List<StorageZone> _zones = new();

    public ZoneRepository(IEnumerable<StorageZone> initialZones)
    {
        _zones.AddRange(initialZones);
    }

    public IReadOnlyCollection<StorageZone> GetAll() => _zones.AsReadOnly();
    public StorageZone? GetById(Guid id) => _zones.FirstOrDefault(z => z.Id == id);
    public void Add(StorageZone entity) => _zones.Add(entity);
    public void Update(StorageZone entity)
    {
        var index = _zones.FindIndex(z => z.Id == entity.Id);
        if (index != -1) _zones[index] = entity;
    }
    public void Delete(Guid id) => _zones.RemoveAll(z => z.Id == id);
}

public class ProductRepository : IRepository<Product, Guid>
{
    private readonly List<Product> _products = new();

    public void Add(Product entity) => _products.Add(entity);
    public IReadOnlyCollection<Product> GetAll() => _products.AsReadOnly();
    public Product? GetById(Guid id) => _products.FirstOrDefault(p => p.Id == id);
    public void Update(Product entity) { }
    public void Delete(Guid id) => _products.RemoveAll(p => p.Id == id);
}