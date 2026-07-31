using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Microsoft.Extensions.DependencyInjection;
using Enset.Infrastructure.Imports.Readers;

namespace Enset.Infrastructure.Imports.Factory;

public class MeterReadingReaderFactory : IMeterReadingReaderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MeterReadingReaderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IMeterReadingReader Create(ImportSourceType type)
    {
        return type switch
        {
            ImportSourceType.Csv => _serviceProvider.GetRequiredService<CsvMeterReadingReader>(),
            // ImportSourceType.Xml => _serviceProvider.GetRequiredService<XmlMeterReadingReader>(),
            _ => throw new NotSupportedException($"Source type {type} not supported")
        };
    }
}