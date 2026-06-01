# Система управління адресним зберіганням складу (Warehouse WMS)

Навчальний реліз стабільної версії `v1.0.0` системи автоматизації складської логістики, побудований на принципах Clean Architecture та SOLID.

## Основна функціональність
* **Адресна сітка складу:** Поділ на зони зберігання з жорстким контролем фізичних лімітів місткості.
* **Бізнес-сценарії (Use Cases):** Оприбуткування вантажів, контроль залишків, аналітика заповненості.
* **Політики відмовостійкості:** Робота через `Result Object`, логування та Retry-поведінка при I/O збоях.

## Навігація по документації
Для отримання детальної інформації перейдіть за відповідними посиланнями:
1. **[USER_GUIDE.md](USER_GUIDE.md)** — інструкція користувача: як запускати додаток, формати введення даних та інтерфейс.
2. **[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)** — посібник розробника: опис архітектурних шарів, SOLID, патернів та розширення.
3. **[TESTING.md](TESTING.md)** — стратегія тестування: запуск Unit/Integration тестів, налаштування Coverlet та збір покриття.
4. **[CHANGELOG.md](CHANGELOG.md)** — історія змін версії `v1.0.0`.
5. **[FINAL_REPORT.md](FINAL_REPORT.md)** — підсумковий технічний звіт по проєкту для захисту лабораторної роботи.

## 🚀Швидкий старт (Команди автоматизації)
```powershell
# Клонування проєкту
git clone [https://github.com/Reddit00/OOP-MiniProject-NefododvNikita.git](https://github.com/Reddit00/OOP-MiniProject-NefododvNikita.git)
cd OOP-MiniProject-NefododvNikita

# Відновлення та збірка
dotnet restore MyProject.sln
dotnet build MyProject.sln --configuration Release

# Запуск тестування
dotnet test MyProject.sln