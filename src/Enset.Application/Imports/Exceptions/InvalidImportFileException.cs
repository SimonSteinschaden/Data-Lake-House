namespace Enset.Application.Imports.Exceptions;

public sealed class InvalidImportFileException : Exception
{
    public InvalidImportFileException(string message)
        : base(message)
    {
    }

    public InvalidImportFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
