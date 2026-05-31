using System;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Application;

namespace MyProject.Application;

public class ShipProductUseCase
{
    private readonly IWarehouseRepository _repository;

    public ShipProductUseCase(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<StorageZone>> ExecuteAsync(Guid zoneId, Guid productId, int quantity)
    {
        if (quantity <= 0) return Result<StorageZone>.Failure("Кількість повинна бути > 0. [Rule_1]");

        var zone = await _repository.GetZoneByIdAsync(zoneId);
        if (zone == null) return Result<StorageZone>.Failure("Вказану комірку не знайдено.");

        var product = await _repository.GetProductByIdAsync(productId);
        if (product == null) return Result<StorageZone>.Failure("Товар не знайдено.");

        try
        {
            zone.RemoveProduct(product, quantity); 
            await _repository.SaveChangesAsync();
            return Result<StorageZone>.Success(zone);
        }
        catch (Exception ex)
        {
            return Result<StorageZone>.Failure(ex.Message);
        }
    }
}