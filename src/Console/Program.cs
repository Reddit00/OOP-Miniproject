using System;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Domain.Extensions;
using MyProject.Application;
using MyProject.Infrastructure;

namespace MyProject.ConsoleApp;

class Program
{
    static void OnWarehouseWarning(StorageZone zone, double occupancyPercentage)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[OBSERVER NOTIFICATION] Попередження критичної місткості!");
        Console.WriteLine($"-> Комірка {zone.Address.Sector}-{zone.Address.Shelf}-{zone.Address.Level} заповнена на {occupancyPercentage:F1}%!");
        Console.ResetColor();
    }

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        var repository = new JsonWarehouseRepository();
        var receiveUseCase = new ReceiveProductUseCase(repository);
        var shipUseCase = new ShipProductUseCase(repository);
        var transferUseCase = new TransferProductUseCase(repository);
        var analyticsService = new WarehouseAnalyticsService(repository);
        Guid laptopId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid generatorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("СИСТЕМА УПРАВЛІННЯ СКЛАДОМ - Ітерація 2");
            Console.ResetColor();
            Console.WriteLine("1.[Бізнес-операція] Автоматично оприбуткувати товар (Приймання)");
            Console.WriteLine("2.[Бізнес-операція] Відвантажити товар з конкретної комірки");
            Console.WriteLine("3.Бізнес-операція] Внутрішньоскладське переміщення між зонами");
            Console.WriteLine("4.[LINQ Запити] Відкрити аналітичне меню та звіти");
            Console.WriteLine("5.Вийти з програми (Зберегти стан)");
            Console.Write("\nОберіть дію: ");

            string? choice = Console.ReadLine();
            if (choice == "5")
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("\nВсі зміни зафіксовано в JSON. Завершення роботи. Гарного дня!");
                Console.ResetColor();
                break;
            }

            switch (choice)
            {
                case "1":
                    Console.Clear();
                    Console.WriteLine("ОПРИБУТКУВАННЯ НОВОЇ ПАРТІЇ ТОВАРУ");
                    Console.WriteLine("1. Ігровий ноутбук (2.5 кг)");
                    Console.WriteLine("2. Промисловий генератор (15.0 кг)");
                    Console.Write("Оберіть товар: ");
                    string? pChoice = Console.ReadLine();
                    Guid selectedProdId = pChoice == "2" ? generatorId : laptopId;

                    Console.Write("Введіть кількість одиниць: ");
                    if (int.TryParse(Console.ReadLine(), out int qtyReceive))
                    {
                        Console.WriteLine("\n[Система] Робота патерну Strategy... Авто-підбір найкращого стелажа...");
                        var result = await receiveUseCase.ExecuteAsync(selectedProdId, qtyReceive, "fast", OnWarehouseWarning);

                        if (result.IsSuccess)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            var z = result.Value!;
                            Console.WriteLine($"\n[УСПІХ] Товар автоматично розміщено в Зону {z.Address.Sector}-{z.Address.Shelf}-{z.Address.Level}. Вага: {z.CurrentWeight}/{z.MaxCapacityWeight} кг ({z.GetOccupancyPercentage():F1}%)");
                        }
                        else ShowError(result.ErrorMessage);
                    }
                    else ShowError("Введено некоректне число.");
                    break;

                case "2": 
                    Console.Clear();
                    Console.WriteLine("ВІДВАНТАЖЕННЯ ТОВАРУ ЗІ СКЛАДУ");
                    var allZonesForShip = await repository.GetAllZonesAsync();
                    
                    Console.WriteLine("\nПоточні залишки на складі:");
                    foreach(var z in allZonesForShip.Where(z => z.Items.Any()))
                    {
                        Console.WriteLine($"-> Зона ID: [{z.Id}] (Адреса: {z.Address.Sector}-{z.Address.Shelf})");
                        foreach(var item in z.Items) Console.WriteLine($"   * Товар ID: [{item.Key}] — Кількість: {item.Value} шт.");
                    }

                    Console.Write("\nВведіть ID Зони, з якої забираємо товар: ");
                    if (Guid.TryParse(Console.ReadLine(), out Guid shipZoneId))
                    {
                        Console.Write("Введіть ID Товару: ");
                        if (Guid.TryParse(Console.ReadLine(), out Guid shipProdId))
                        {
                            Console.Write("Введіть кількість одиниць для списання: ");
                            if (int.TryParse(Console.ReadLine(), out int qtyShip))
                            {
                                var result = await shipUseCase.ExecuteAsync(shipZoneId, shipProdId, qtyShip);
                                if (result.IsSuccess)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"\n[УСПІХ] Товар успішно відвантажено! Зміни збережено в JSON.");
                                }
                                else ShowError(result.ErrorMessage);
                            }
                        }
                    }
                    break;

                case "3":
                    Console.Clear();
                    Console.WriteLine("ВНУТРІШНЬОСКЛАДСЬКЕ ПЕРЕМІЩЕННЯ ТОВАРУ");
                    Console.Write("Введіть ID Зони-ОДЕРЖУВАЧА (Куди переносимо): ");
                    if (Guid.TryParse(Console.ReadLine(), out Guid targetZoneId))
                    {
                        Console.Write("Введіть ID Зони-ВІДПРАВНИКА (Звідки забираємо): ");
                        if (Guid.TryParse(Console.ReadLine(), out Guid sourceZoneId))
                        {
                            Console.Write("Введіть ID Товару: ");
                            if (Guid.TryParse(Console.ReadLine(), out Guid transProdId))
                            {
                                Console.Write("Введіть кількість для перенесення: ");
                                if (int.TryParse(Console.ReadLine(), out int qtyTrans))
                                {
                                    var result = await transferUseCase.ExecuteAsync(sourceZoneId, targetZoneId, transProdId, qtyTrans);
                                    if (result.IsSuccess)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"\n[УСПІХ] {result.Value}");
                                    }
                                    else ShowError(result.ErrorMessage);
                                }
                            }
                        }
                    }
                    break;

                case "4":
                    await RunAnalyticsMenuAsync(analyticsService, laptopId);
                    break;

                default:
                    ShowError("Некоректний вибір пункту меню.");
                    break;
            }

            Console.ResetColor();
            Console.WriteLine("\nНатисніть будь-яку клавішу для повернення в головне меню...");
            Console.ReadKey();
        }
    }

    private static async Task RunAnalyticsMenuAsync(WarehouseAnalyticsService analytics, Guid laptopId)
    {
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("МЕНЮ АНАЛІТИЧНИХ ЗАПИТІВ ТА LINQ");
            Console.ResetColor();
            Console.WriteLine("1. [LINQ 1] Пошук зон із критичним рівнем завантаження (>= 85%)");
            Console.WriteLine("2. [LINQ 2] Мульти-пошук: Локації розміщення 'Ігрових ноутбуків'");
            Console.WriteLine("3. [LINQ 3] Топ найвільніших зон для важких вантажів");
            Console.WriteLine("4. [LINQ 4] Переглянути групування адрес комірок за секторами");
            Console.WriteLine("5. [АГРЕГАЦІЯ] Загальна статистика місткості всього складу");
            Console.WriteLine("6. Повернутися до головного меню");
            Console.Write("\nОберіть запит: ");

            string? subChoice = Console.ReadLine();
            if (subChoice == "6") break;

            Console.WriteLine("\nРЕЗУЛЬТАТ ЗАПИТУ ");
            switch (subChoice)
            {
                case "1":
                    var critical = await analytics.GetCriticalZonesAsync();
                    if (!critical.Any()) Console.WriteLine("Критично переповнених зон не знайдено.");
                    foreach (var z in critical) Console.WriteLine($"Зона {z.Address.Sector}-{z.Address.Shelf}: {z.CurrentWeight}/{z.MaxCapacityWeight} кг ({z.GetOccupancyPercentage():F1}%)");
                    break;

                case "2":
                    var locations = await analytics.FindProductLocationsAsync(laptopId);
                    if (!locations.Any()) Console.WriteLine("Цього товару зараз немає на складі.");
                    foreach (dynamic loc in locations) Console.WriteLine($"📍 Комірка {loc.ZoneAddress} | Кількість: {loc.Quantity} шт. | Вага: {loc.OccupiedWeight} кг");
                    break;

                case "3":
                    var freeHeavy = await analytics.GetTopFreeZonesForHeavyLoadsAsync();
                    foreach (var z in freeHeavy) Console.WriteLine($"Посилена зона {z.Address.Sector}-{z.Address.Shelf} | Вільне місце: {z.MaxCapacityWeight - z.CurrentWeight} кг з {z.MaxCapacityWeight} кг");
                    break;

                case "4":
                    var grouped = await analytics.GetZoneAddressesGroupedBySectorAsync();
                    foreach (var group in grouped) Console.WriteLine($"Box Сектор [{group.Key}]: " + string.Join(", ", group));
                    break;

                case "5":
                    var stats = await analytics.GetWarehouseSummaryAsync();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"ЗАГАЛЬНИЙ СТАН СХОВИЩА:");
                    Console.WriteLine($"-> Загальний тонаж складу: {stats.TotalCurrentWeight:F1} кг / {stats.TotalMaxCapacity:F1} кг");
                    Console.WriteLine($"-> Сумарна завантаженість об'єкта: {stats.GeneralOccupancyPercentage:F1}%");
                    Console.WriteLine($"-> Всього одиниць товарів на зберіганні: {stats.TotalUnitsStored} шт.");
                    break;

                default:
                    Console.WriteLine("Некоректний вибір.");
                    break;
            }
            Console.ResetColor();
            Console.WriteLine("\nНатисніть клавішу для продовження...");
            Console.ReadKey();
        }
    }

    private static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[ПОМИЛКА ОПЕРАЦІЇ] {message}");
        Console.ResetColor();
    }
}