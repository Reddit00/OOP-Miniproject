using System;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Application;

namespace MyProject.Application;

public class TransferProductUseCase
{
    private readonly IWarehouseRepository _repository;

    public TransferProductUseCase(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<string>> ExecuteAsync(Guid sourceZoneId, Guid targetZoneId, Guid productId, int quantity)
    {
        if (quantity <= 0) return Result<string>.Failure("Кількість повинна бути > 0. [Rule_1]");
        if (sourceZoneId == targetZoneId) return Result<string>.Failure("Комірка відправника та одержувача збігаються.");

        var sourceZone = await _repository.GetZoneByIdAsync(sourceZoneId);
        var targetZone = await _repository.GetZoneByIdAsync(targetZoneId);

        if (sourceZone == null || targetZone == null) 
            return Result<string>.Failure("Одну з комірок не знайдено.");

        var product = await _repository.GetProductByIdAsync(productId);
        if (product == null) return Result<string>.Failure("Товар не знайдено.");

        try
        {
            sourceZone.RemoveProduct(product, quantity);
            targetZone.AddProduct(product, quantity);
            await _repository.SaveChangesAsync();

            return Result<string>.Success($"Успішно переміщено {quantity} шт. з комірки {sourceZone.Address.Sector} до комірки {targetZone.Address.Sector}");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Помилка переміщення: {ex.Message}");
        }
    }
}