# Посібник розробника (Developer Guide)

## Архітектура проєкту (Clean Architecture)

Проєкт розділено на незалежні шари відповідно до принципів чистої архітектури:
1. **Domain:** Сутності (`StorageZone`, `Product`), Value Objects (`SKU`, `ZoneAddress`), інтерфейси розширення (`IPlacementStrategy`). Не має жодних зовнішніх залежностей.
2. **Application:** Сценарії (`ReceiveProductUseCase`), доменні сервіси (`WarehouseAnalyticsService`) та інтерфейси сховища (`IWarehouseRepository`).
3. **Infrastructure / Tests:** Фізична серіалізація на диск у JSON, логування та автоматизовані тести.

## Правила розширення системи (Паттерн Strategy)

Для зміни алгоритму пошуку оптимального місця для товару (наприклад, оптимізація під швидкість доступу або під балансування ваги):
1. Створіть новий клас у шарі домену, який реалізує інтерфейс `IPlacementStrategy`.
2. Реалізуйте метод `FindZone`.
3. Передайте нову стратегію в конструктор `ReceiveProductUseCase` через Dependency Injection. Старий код юзкейсу міняти не потрібно (**Open/Closed Principle**).

## 🧪 Запуск тестів
Тести запускаються через CLI: `dotnet test MyProject.sln`. Шари ізольовані за допомогою архітектурних швів (Seams), що дозволяє тестувати Use Cases через фейки в пам'яті (`AnalyticsFakeRepository`), не зачіпаючи диск.