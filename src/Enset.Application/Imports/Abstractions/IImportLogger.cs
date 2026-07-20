namespace Enset.Application.Imports.Abstractions;

public interface IImportLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Error(string message, Exception exception) =>
        Error($"{message} {exception}");
}
