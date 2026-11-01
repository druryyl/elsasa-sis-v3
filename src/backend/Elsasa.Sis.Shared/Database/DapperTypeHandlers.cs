namespace Elsasa.Sis.Shared.Database;

using System.Data;

using Dapper;

/// <summary>
/// Dapper type handler that maps <see cref="DateOnly"/> to SQL Server <c>DATE</c>.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    /// <inheritdoc />
    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            null or DBNull => throw new ArgumentException(
                "Cannot convert a NULL database value to DateOnly.",
                nameof(value)),
            _ => throw new ArgumentException(
                $"Cannot convert a database value of type '{value.GetType()}' to DateOnly.",
                nameof(value)),
        };
    }
}

/// <summary>
/// Dapper type handler that maps <see cref="TimeOnly"/> to SQL Server <c>TIME</c>.
/// </summary>
public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }

    /// <inheritdoc />
    public override TimeOnly Parse(object value)
    {
        return value switch
        {
            TimeOnly timeOnly => timeOnly,
            TimeSpan timeSpan => TimeOnly.FromTimeSpan(timeSpan),
            DateTime dateTime => TimeOnly.FromDateTime(dateTime),
            null or DBNull => throw new ArgumentException(
                "Cannot convert a NULL database value to TimeOnly.",
                nameof(value)),
            _ => throw new ArgumentException(
                $"Cannot convert a database value of type '{value.GetType()}' to TimeOnly.",
                nameof(value)),
        };
    }
}

/// <summary>
/// Registers the shared Dapper type handlers in <see cref="SqlMapper"/>'s global registry.
/// </summary>
public static class DapperTypeHandlers
{
    private static readonly object _registrationLock = new();
    private static bool _isRegistered;

    /// <summary>
    /// Registers <see cref="DateOnlyTypeHandler"/> and <see cref="TimeOnlyTypeHandler"/>.
    /// Calling this method more than once is safe.
    /// </summary>
    /// <remarks>
    /// Idempotency is achieved with a process-wide registration flag guarded by a
    /// lock. Dapper 2.1 exposes no public <c>RemoveTypeHandler</c>, and
    /// <c>AddTypeHandler</c> would silently replace the previous registration;
    /// skipping repeated registration is therefore the simplest guaranteed-safe
    /// option. The handlers are stateless, so a single shared instance pair is
    /// sufficient for the lifetime of the process.
    /// </remarks>
    public static void RegisterTypeHandlers()
    {
        lock (_registrationLock)
        {
            if (_isRegistered)
            {
                return;
            }

            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

            _isRegistered = true;
        }
    }
}
