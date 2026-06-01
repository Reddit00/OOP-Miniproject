using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyProject.Domain;

namespace MyProject.Application;

public interface IWarehouseReadOnlyRepository
{
    Task<IEnumerable<StorageZone>> GetAllZonesAsync();
    Task<StorageZone?> GetZoneByIdAsync(Guid id);
    Task<Product?> GetProductByIdAsync(Guid id);
}

public interface IWarehouseWritableRepository : IWarehouseReadOnlyRepository
{
    Task SaveChangesAsync();
}

public interface IWarehouseRepository : IWarehouseWritableRepository { }