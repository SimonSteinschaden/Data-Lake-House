using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.DTOs;
using Enset.Domain.Energy;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Imports.Mappings;

/*
public class MeterReadingMapper : IMeterReadingMapper
{
    
    private readonly EnsetDbContext _context;

    public MeterReadingMapper(EnsetDbContext context)
    {
        _context = context;
    }

    public async Task<MeterReading> MapAsync(
        MeterReadingImportDto dto,
        CancellationToken cancellationToken = default)
    {
        var meter = await _context.Meters
            .FirstOrDefaultAsync(m => m.MeterNumber == dto.MeterNumber, cancellationToken);

        if (meter == null)
        {
            meter = new Meter
            {
                MeterNumber = dto.MeterNumber,
                Unit = dto.Unit ?? "kWh",
                BuildingId = dto.BuildingId,
                Type = MeterType.Consumption // default
            };
            _context.Meters.Add(meter);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new MeterReading
        {
            MeterId = meter.Id,
            Timestamp = dto.Timestamp,
            Value = dto.Value,
            Unit = dto.Unit ?? meter.Unit,
            QualityFlag = dto.QualityFlag.HasValue ? (DataQuality)dto.QualityFlag.Value : DataQuality.Validated,
            BuildingId = dto.BuildingId ?? meter.BuildingId,
            CustomerId = dto.CustomerId
        };
    }
    
}
*/
