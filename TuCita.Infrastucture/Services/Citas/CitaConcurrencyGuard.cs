using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TuCita.Application.Common;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Citas;

internal static class CitaConcurrencyGuard
{
    private const int LockTimeoutMilliseconds = 10000;

    public static async Task<ServiceResult<T>> ExecuteWithBusinessScheduleLockAsync<T>(
        ReservaFlowDbContext dbContext,
        int idNegocio,
        Func<Task<ServiceResult<T>>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var lockResult = await AcquireBusinessScheduleLockAsync(dbContext, idNegocio, cancellationToken);
        if (lockResult < 0)
        {
            return ServiceResult<T>.Validation([
                new ValidationError(
                    string.Empty,
                    "No se pudo bloquear la agenda para confirmar la disponibilidad. Intenta nuevamente.")
            ]);
        }

        var result = await operation();
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static async Task<int> AcquireBusinessScheduleLockAsync(
        ReservaFlowDbContext dbContext,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var result = new SqlParameter("@result", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        {
            Value = $"TuCita:Agenda:Negocio:{idNegocio}"
        };
        var timeout = new SqlParameter("@timeout", SqlDbType.Int)
        {
            Value = LockTimeoutMilliseconds
        };

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = @timeout;
            """,
            [result, resource, timeout],
            cancellationToken);

        return result.Value is int value ? value : Convert.ToInt32(result.Value);
    }
}
