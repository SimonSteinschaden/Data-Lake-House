namespace Enset.Application.Imports.Abstractions;

public interface IImportLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
