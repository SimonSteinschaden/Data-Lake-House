using System.Data;
using Enset.Application.Crud;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Enset.Infrastructure.Crud;

public sealed class EfBuildingNumberGenerator(EnsetDbContext db) :
    IBuildingNumberGenerator
{
    private static long _nonRelationalSequence;

    public async Task<string> NextAsync(CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
        {
            var value = Interlocked.Increment(ref _nonRelationalSequence);
            return $"BLD-{value:000000}";
        }

        var connection = db.Database.GetDbConnection();
        var closeAfterRead = connection.State != ConnectionState.Open;
        if (closeAfterRead)
            await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            if (db.Database.CurrentTransaction is { } transaction)
                command.Transaction = transaction.GetDbTransaction();
            command.CommandText =
                """
                SELECT 'BLD-' ||
                       LPAD(nextval('"BuildingNumberSequence"')::text, 6, '0')
                """;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result as string ??
                throw new InvalidOperationException(
                    "Die nächste Gebäudenummer konnte nicht erzeugt werden.");
        }
        finally
        {
            if (closeAfterRead)
                await connection.CloseAsync();
        }
    }
}
