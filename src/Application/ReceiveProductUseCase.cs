using System;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Application;

namespace MyProject.Application;

public class ReceiveProductUseCase
{
    private readonly IWarehouseRepository _repository;

    public ReceiveProductUseCase(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<StorageZone>> ExecuteAsync(Guid productId, int quantity, string strategyType, CapacityWarningHandler observer)
    {
        if (quantity <= 0) return Result<StorageZone>.Failure("Кількість повинна бути > 0. [Rule_1]");

        var product = await _repository.GetProductByIdAsync(productId);
        if (product == null) return Result<StorageZone>.Failure("Товар не знайдено.");

        var allZones = await _repository.GetAllZonesAsync();
        var targetZone = allZones
        .Where(z => z.MaxCapacityWeight - z.CurrentWeight >= product.Weight * quantity)
        .OrderByDescending(z => z.MaxCapacityWeight - z.CurrentWeight)
        .FirstOrDefault();

        if (targetZone == null) return Result<StorageZone>.Failure("Немає вільної комірки під таку вагу. [Rule_2]");

        try
        {
            targetZone.OnCapacityWarning += observer; 
            targetZone.AddProduct(product, quantity);
            targetZone.OnCapacityWarning -= observer;

            await _repository.SaveChangesAsync();
            return Result<StorageZone>.Success(targetZone);
        }
        catch (Exception ex)
        {
            return Result<StorageZone>.Failure(ex.Message);
        }
    }
}