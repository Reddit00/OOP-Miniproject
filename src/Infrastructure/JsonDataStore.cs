using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MyProject.Domain;
using MyProject.Application.Common;

namespace MyProject.Infrastructure;

public class JsonDataStore : IDataStore<StorageZone>
{
    private readonly string _filePath = "warehouse_storage.json";

    public async Task<IReadOnlyCollection<StorageZone>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<StorageZone>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var data = JsonSerializer.Deserialize<List<StorageZone>>(json);
            return data ?? new List<StorageZone>();
        }
        catch (JsonException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[КРИТИЧНО] Файл {_filePath} пошкоджено! Стан скинуто до початкового.");
            Console.ResetColor();
            return Array.Empty<StorageZone>();
        }
    }

    public async Task SaveAsync(IReadOnlyCollection<StorageZone> items, CancellationToken cancellationToken = default)
    {
        int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(items, options);
                await File.WriteAllTextAsync(_filePath, json, cancellationToken);
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                await Task.Delay(200, cancellationToken);
            }
        }
    }
}