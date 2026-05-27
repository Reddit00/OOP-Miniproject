using MyProject.Domain;
using MyProject.Application;

namespace MyProject.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
       
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        IWarehouseRepository repository = new InMemoryWarehouseRepository();
        ReceiveProductUseCase receiveUseCase = new ReceiveProductUseCase(repository);

        Guid defaultProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid defaultZoneId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid workerId = Guid.NewGuid();

        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("   СИСТЕМА УПРАВЛІННЯ СКЛАДОМ (Thule-WMS) — Ітерація 1 ");
            Console.ResetColor();
            Console.WriteLine("Доступні операції:");
            Console.WriteLine("1. Прийняти товар 'Ігровий ноутбук' (2.5 кг) на Сектор А-1-1 [Ліміт: 20 кг]");
            Console.WriteLine("2. Вийти з програми");
            Console.Write("\nОберіть номер дії: ");

            string? choice = Console.ReadLine();

            if (choice == "2")
            {
                Console.WriteLine("Завершення роботи. Гарного дня!");
                break;
            }

            if (choice == "1")
            {
                Console.Write("\nВведіть кількість одиниць товару для оприбуткування: ");
                string? inputQty = Console.ReadLine();

                if (int.TryParse(inputQty, out int quantity))
                {
                    try
                    {
                        Console.WriteLine("\n[Система] Обробка операції шарами архітектури...");
                        
                        await receiveUseCase.ExecuteAsync(workerId, defaultProductId, defaultZoneId, quantity);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n[УСПІХ] Товар успішно додано! Обмеження місткості зони не порушено.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n[ВІДХИЛЕНО ДОМЕНОМ] Операція заблокована бізнес-правилом:");
                        Console.WriteLine($"-> {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n[ПОМИЛКА СИСТЕМИ] {ex.Message}");
                    }
                    finally
                    {
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\nПомилка: Введено некоректне числове значення.");
                    Console.ResetColor();
                }

                Console.WriteLine("\nНатисніть будь-яку клавішу для повернення в меню...");
                Console.ReadKey();
            }
        }
    }
}