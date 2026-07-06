using Enset.Application.Imports.DTOs;
using Enset.Domain.Energy;

namespace Enset.Application.Imports.Abstractions;

public interface IMeterReadingMapper
{
    Task<MeterReading> MapAsync(
        MeterReadingImportDto dto,
        CancellationToken cancellationToken = default);
}