using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Application;

namespace MyProject.Tests;

public class FakeWarehouseRepository : IWarehouseWritableRepository
{
    public List<StorageZone> Zones { get; } = new();
    public List<Product> Products { get; } = new();

    public Task<IEnumerable<StorageZone>> GetAllZonesAsync() => Task.FromResult<IEnumerable<StorageZone>>(Zones);

    public Task<StorageZone?> GetZoneByIdAsync(Guid id) => Task.FromResult(Zones.FirstOrDefault(z => z.Id == id));

    public Task<Product?> GetProductByIdAsync(Guid id) => Task.FromResult(Products.FirstOrDefault(p => p.Id == id));

    public Task SaveChangesAsync() => Task.CompletedTask; 
}