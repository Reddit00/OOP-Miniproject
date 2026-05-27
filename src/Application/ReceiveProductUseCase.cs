using MyProject.Domain;
namespace MyProject.Application;

public class ReceiveProductUseCase
{
    private readonly IWarehouseRepository _repository;
    public ReceiveProductUseCase(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(Guid workerId, Guid productId, Guid zoneId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Кількість товару для оприбуткування повинна бути більшою за 0.");

        var zone = await _repository.GetZoneByIdAsync(zoneId);
        if (zone == null)
            throw new KeyNotFoundException("Зазначену зону зберігання не знайдено на складі.");

        var product = await _repository.GetProductByIdAsync(productId);
        if (product == null)
            throw new KeyNotFoundException("Товар з таким ID не зареєстрований у каталозі.");

        zone.AddProduct(product, quantity);
        await _repository.SaveChangesAsync();
    }
}
