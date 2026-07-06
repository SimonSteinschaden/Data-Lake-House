using Enset.Application.Imports.Abstractions;

namespace Enset.Worker.Logging;

public sealed class ConsoleImportLogger : IImportLogger
{
    public void Info(string message)
    {
        Console.WriteLine($"[INFO ] {message}");
    }

    public void Warning(string message)
    {
        Console.WriteLine($"[WARN ] {message}");
    }

    public void Error(string message)
    {
        Console.Error.WriteLine($"[ERROR] {message}");
    }
}