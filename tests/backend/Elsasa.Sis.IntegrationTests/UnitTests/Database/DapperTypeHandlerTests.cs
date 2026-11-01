namespace Elsasa.Sis.IntegrationTests.UnitTests.Database;

using System.Data;

using Dapper;

using Elsasa.Sis.Shared.Database;

using Microsoft.Data.SqlClient;

/// <summary>
/// Deterministic unit tests for the shared Dapper <see cref="DateOnly"/> and
/// <see cref="TimeOnly"/> type handlers. No SQL Server instance is required.
/// </summary>
public sealed class DapperTypeHandlerTests
{
    public DapperTypeHandlerTests()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    [Fact]
    public void RegisterTypeHandlers_Should_RegisterBothHandlers_When_Called()
    {
        Assert.True(SqlMapper.HasTypeHandler(typeof(DateOnly)));
        Assert.True(SqlMapper.HasTypeHandler(typeof(TimeOnly)));
    }

    [Fact]
    public void RegisterTypeHandlers_Should_NotThrow_When_CalledTwice()
    {
        var exception = Record.Exception(DapperTypeHandlers.RegisterTypeHandlers);

        Assert.Null(exception);
        Assert.True(SqlMapper.HasTypeHandler(typeof(DateOnly)));
        Assert.True(SqlMapper.HasTypeHandler(typeof(TimeOnly)));
    }

    [Fact]
    public void DateOnlyTypeHandler_SetValue_Should_SetDateDbTypeAndDateTimeValue_When_CalledDirectly()
    {
        var handler = new DateOnlyTypeHandler();
        var parameter = new SqlParameter();
        var value = new DateOnly(2026, 11, 1);

        handler.SetValue(parameter, value);

        Assert.Equal(DbType.Date, parameter.DbType);
        var dateTime = Assert.IsType<DateTime>(parameter.Value);
        Assert.Equal(value, DateOnly.FromDateTime(dateTime));
    }

    [Fact]
    public void DateOnlyTypeHandler_SetValue_Should_SetDateDbTypeAndDateTimeValue_When_CalledThroughDapperInterface()
    {
        SqlMapper.ITypeHandler handler = new DateOnlyTypeHandler();
        var parameter = new SqlParameter();
        var value = new DateOnly(2026, 11, 1);

        handler.SetValue(parameter, value);

        Assert.Equal(DbType.Date, parameter.DbType);
        var dateTime = Assert.IsType<DateTime>(parameter.Value);
        Assert.Equal(value, DateOnly.FromDateTime(dateTime));
    }

    [Fact]
    public void DateOnlyTypeHandler_SetValue_Should_SetDBNull_When_ValueIsDBNullViaDapperInterface()
    {
        SqlMapper.ITypeHandler handler = new DateOnlyTypeHandler();
        var parameter = new SqlParameter();

        handler.SetValue(parameter, DBNull.Value);

        Assert.Equal(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void DateOnlyTypeHandler_SetValue_Should_ThrowArgumentNullException_When_ParameterIsNull()
    {
        var handler = new DateOnlyTypeHandler();

        Assert.Throws<ArgumentNullException>(() => handler.SetValue(null!, new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void DateOnlyTypeHandler_Parse_Should_ReturnDateOnly_When_ValueIsDateTime()
    {
        var handler = new DateOnlyTypeHandler();
        var databaseValue = new DateTime(2026, 11, 1, 15, 30, 0, DateTimeKind.Unspecified);

        var parsed = handler.Parse(databaseValue);

        Assert.Equal(new DateOnly(2026, 11, 1), parsed);
    }

    [Fact]
    public void DateOnlyTypeHandler_Parse_Should_ReturnDateOnly_When_CalledThroughDapperInterface()
    {
        SqlMapper.ITypeHandler handler = new DateOnlyTypeHandler();
        var databaseValue = new DateTime(2026, 11, 1, 15, 30, 0, DateTimeKind.Unspecified);

        var parsed = handler.Parse(typeof(DateOnly), databaseValue);

        Assert.Equal(new DateOnly(2026, 11, 1), parsed);
    }

    [Fact]
    public void DateOnlyTypeHandler_Parse_Should_ReturnDateOnly_When_ValueIsDateOnly()
    {
        var handler = new DateOnlyTypeHandler();
        var expected = new DateOnly(2026, 11, 1);

        var parsed = handler.Parse(expected);

        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void DateOnlyTypeHandler_Parse_Should_Throw_When_ValueIsDBNull()
    {
        var handler = new DateOnlyTypeHandler();

        Assert.Throws<ArgumentException>(() => handler.Parse(DBNull.Value));
    }

    [Fact]
    public void DateOnlyTypeHandler_Parse_Should_Throw_When_ValueIsNull()
    {
        var handler = new DateOnlyTypeHandler();

        Assert.Throws<ArgumentException>(() => handler.Parse(null!));
    }

    [Fact]
    public void DateOnlyTypeHandler_Parse_Should_Throw_When_ValueIsUnsupportedType()
    {
        var handler = new DateOnlyTypeHandler();

        Assert.Throws<ArgumentException>(() => handler.Parse("2026-11-01"));
    }

    [Fact]
    public void TimeOnlyTypeHandler_SetValue_Should_SetTimeDbTypeAndTimeSpanValue_When_CalledDirectly()
    {
        var handler = new TimeOnlyTypeHandler();
        var parameter = new SqlParameter();
        var value = new TimeOnly(13, 45, 30);

        handler.SetValue(parameter, value);

        Assert.Equal(DbType.Time, parameter.DbType);
        var timeSpan = Assert.IsType<TimeSpan>(parameter.Value);
        Assert.Equal(value, TimeOnly.FromTimeSpan(timeSpan));
    }

    [Fact]
    public void TimeOnlyTypeHandler_SetValue_Should_SetTimeDbTypeAndTimeSpanValue_When_CalledThroughDapperInterface()
    {
        SqlMapper.ITypeHandler handler = new TimeOnlyTypeHandler();
        var parameter = new SqlParameter();
        var value = new TimeOnly(13, 45, 30);

        handler.SetValue(parameter, value);

        Assert.Equal(DbType.Time, parameter.DbType);
        var timeSpan = Assert.IsType<TimeSpan>(parameter.Value);
        Assert.Equal(value, TimeOnly.FromTimeSpan(timeSpan));
    }

    [Fact]
    public void TimeOnlyTypeHandler_SetValue_Should_SetDBNull_When_ValueIsDBNullViaDapperInterface()
    {
        SqlMapper.ITypeHandler handler = new TimeOnlyTypeHandler();
        var parameter = new SqlParameter();

        handler.SetValue(parameter, DBNull.Value);

        Assert.Equal(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void TimeOnlyTypeHandler_SetValue_Should_ThrowArgumentNullException_When_ParameterIsNull()
    {
        var handler = new TimeOnlyTypeHandler();

        Assert.Throws<ArgumentNullException>(() => handler.SetValue(null!, new TimeOnly(13, 45, 30)));
    }

    [Fact]
    public void TimeOnlyTypeHandler_Parse_Should_ReturnTimeOnly_When_ValueIsTimeSpan()
    {
        var handler = new TimeOnlyTypeHandler();
        var databaseValue = new TimeSpan(13, 45, 30);

        var parsed = handler.Parse(databaseValue);

        Assert.Equal(new TimeOnly(13, 45, 30), parsed);
    }

    [Fact]
    public void TimeOnlyTypeHandler_Parse_Should_ReturnTimeOnly_When_CalledThroughDapperInterface()
    {
        SqlMapper.ITypeHandler handler = new TimeOnlyTypeHandler();
        var databaseValue = new TimeSpan(13, 45, 30);

        var parsed = handler.Parse(typeof(TimeOnly), databaseValue);

        Assert.Equal(new TimeOnly(13, 45, 30), parsed);
    }

    [Fact]
    public void TimeOnlyTypeHandler_Parse_Should_ReturnTimeOnly_When_ValueIsTimeOnly()
    {
        var handler = new TimeOnlyTypeHandler();
        var expected = new TimeOnly(13, 45, 30);

        var parsed = handler.Parse(expected);

        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void TimeOnlyTypeHandler_Parse_Should_ReturnTimeOnly_When_ValueIsDateTime()
    {
        var handler = new TimeOnlyTypeHandler();
        var databaseValue = new DateTime(2026, 11, 1, 8, 15, 0, DateTimeKind.Unspecified);

        var parsed = handler.Parse(databaseValue);

        Assert.Equal(new TimeOnly(8, 15, 0), parsed);
    }

    [Fact]
    public void TimeOnlyTypeHandler_Parse_Should_Throw_When_ValueIsDBNull()
    {
        var handler = new TimeOnlyTypeHandler();

        Assert.Throws<ArgumentException>(() => handler.Parse(DBNull.Value));
    }

    [Fact]
    public void TimeOnlyTypeHandler_Parse_Should_Throw_When_ValueIsNull()
    {
        var handler = new TimeOnlyTypeHandler();

        Assert.Throws<ArgumentException>(() => handler.Parse(null!));
    }

    [Fact]
    public void TimeOnlyTypeHandler_Parse_Should_Throw_When_ValueIsUnsupportedType()
    {
        var handler = new TimeOnlyTypeHandler();

        Assert.Throws<ArgumentException>(() => handler.Parse("13:45:30"));
    }
}
