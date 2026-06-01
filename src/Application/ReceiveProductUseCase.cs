using System;
using System.Threading.Tasks;
using MyProject.Domain;

namespace MyProject.Application;

public class ReceiveProductUseCase
{
    private readonly IWarehouseWritableRepository _repository;
    private readonly IPlacementStrategy _placementStrategy;

    public ReceiveProductUseCase(IWarehouseWritableRepository repository, IPlacementStrategy placementStrategy)
    {
        _repository = repository;
        _placementStrategy = placementStrategy;
    }

    public async Task<Result> ExecuteAsync(Guid productId, int quantity)
    {
        if (quantity <= 0) return Result.Failure("Кількість повинна бути > 0");

        var allZones = await _repository.GetAllZonesAsync();
        var product = await _repository.GetProductByIdAsync(productId);

        if (product == null) return Result.Failure("Товар не знайдено");

        var targetZone = _placementStrategy.FindZone(allZones, product, quantity);
        if (targetZone == null) return Result.Failure("Немає вільної комірки для такого об'єму");

        try
        {
            targetZone.AddProduct(product, quantity);
            await _repository.SaveChangesAsync(); 
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}