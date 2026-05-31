using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Domain;

namespace MyProject.Application;

public class WarehouseAnalyticsService
{
    private readonly IWarehouseRepository _repository;

    public WarehouseAnalyticsService(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<StorageZone>> GetCriticalZonesAsync()
    {
        var zones = await _repository.GetAllZonesAsync();
        
        return zones
            .Where(z => z.MaxCapacityWeight > 0 && (z.CurrentWeight / z.MaxCapacityWeight * 100.0) >= 85.0)
            .OrderByDescending(z => z.MaxCapacityWeight > 0 ? (z.CurrentWeight / z.MaxCapacityWeight * 100.0) : 0);
    }

    public async Task<IEnumerable<object>> FindProductLocationsAsync(Guid productId)
    {
        var zones = await _repository.GetAllZonesAsync();

        return zones
            .Where(z => z.Items.ContainsKey(productId))
            .Select(z => new
            {
                ZoneAddress = z.Address?.ToString() ?? "Unknown Address",
                Quantity = z.Items[productId],
                OccupiedWeight = z.CurrentWeight
            });
    }

    public async Task<IEnumerable<StorageZone>> GetTopFreeZonesForHeavyLoadsAsync()
    {
        var zones = await _repository.GetAllZonesAsync();

        return zones
            .Where(z => z.MaxCapacityWeight >= 100.0) 
            .OrderBy(z => z.MaxCapacityWeight > 0 ? (z.CurrentWeight / z.MaxCapacityWeight * 100.0) : 0) 
            .Take(3);
    }

    public async Task<ILookup<string, string>> GetZoneAddressesGroupedBySectorAsync()
    {
        var zones = await _repository.GetAllZonesAsync();

        return zones.ToLookup(
            z => z.Address?.ToString()?.Split('-').FirstOrDefault() ?? "Unknown",
            z => z.Address?.ToString() ?? "Unknown Address"
        );
    }

    public async Task<WarehouseSummaryStats> GetWarehouseSummaryAsync()
    {
        var zones = await _repository.GetAllZonesAsync();

        double totalMaxCapacity = zones.Sum(z => z.MaxCapacityWeight);
        double totalCurrentWeight = zones.Sum(z => z.CurrentWeight);
        int totalItemsCount = zones.Sum(z => z.Items.Sum(i => i.Value));

        double generalOccupancyPct = totalMaxCapacity > 0 
            ? (totalCurrentWeight / totalMaxCapacity) * 100 
            : 0;

        return new WarehouseSummaryStats(
            totalMaxCapacity,
            totalCurrentWeight,
            generalOccupancyPct,
            totalItemsCount
        );
    }
}

public record WarehouseSummaryStats(
    double TotalMaxCapacity,
    double TotalCurrentWeight,
    double GeneralOccupancyPercentage,
    int TotalUnitsStored
);