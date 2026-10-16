using Dapper;
using System.Data;
using System.Globalization;

namespace JJCore.Infra.Data.Extensions
{
    /// <summary>
    /// Registers Dapper type handlers that adjust how JJCore reads values from specific providers.
    /// </summary>
    /// <remarks>
    /// Dapper type handlers apply to the whole process, including Dapper code that does not use JJCore. The handlers only
    /// change how values are read; parameters keep the types that Dapper sends by default.
    /// </remarks>
    public static class JJCoreTypeHandlers
    {
        private static readonly object SyncRoot = new();
        private static volatile bool _guidHandlerChecked;

        /// <summary>
        /// Reads DateTimeOffset properties from columns that return a DateTime without kind as UTC instead of local time.
        /// </summary>
        /// <remarks>
        /// MySQL has no column type with an offset. MySqlConnector writes a DateTimeOffset as its UTC value, but reads it
        /// back as a DateTime without kind, which Dapper converts to DateTimeOffset as local time. Call this method once at
        /// startup, before the first query, when the application stores DateTimeOffset values in MySQL datetime columns.
        /// The handler applies to every provider in the process: a PostgreSQL timestamp column read into a DateTimeOffset
        /// property is also treated as UTC. Values that already carry an offset are not changed. It does not fix MySQL
        /// timestamp columns, which the server converts using the session time zone.
        /// </remarks>
        public static void ReadDateTimeOffsetAsUtc()
        {
            lock (SyncRoot)
            {
                SqlMapper.AddTypeHandler(new UtcDateTimeOffsetHandler());
                SqlMapper.PurgeQueryCache();
            }
        }

        // Microsoft.Data.Sqlite writes Guid values as TEXT, which Dapper does not convert back to Guid. The handler is
        // registered on the first SQLite operation and never replaces a Guid handler registered by the application.
        internal static void EnsureSQLiteGuidHandler()
        {
            if (_guidHandlerChecked)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_guidHandlerChecked)
                {
                    return;
                }

                if (!SqlMapper.HasTypeHandler(typeof(Guid)))
                {
                    SqlMapper.AddTypeHandler(new TextGuidHandler());
                    SqlMapper.PurgeQueryCache();
                }

                _guidHandlerChecked = true;
            }
        }

        private sealed class TextGuidHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value)
            {
                parameter.DbType = DbType.Guid;
                parameter.Value = value;
            }

            // Binary values are not converted: the byte order depends on how they were written (for example, MySQL
            // UUID_TO_BIN), and a wrong guess would return a different Guid without any error.
            public override Guid Parse(object value)
            {
                return value switch
                {
                    Guid guid => guid,
                    string text => Guid.Parse(text),
                    _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Guid.")
                };
            }
        }

        private sealed class UtcDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
        {
            public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
            {
                parameter.DbType = DbType.DateTimeOffset;
                parameter.Value = value;
            }

            public override DateTimeOffset Parse(object value)
            {
                return value switch
                {
                    DateTimeOffset dateTimeOffset => dateTimeOffset,
                    DateTime dateTime when dateTime.Kind == DateTimeKind.Local => new DateTimeOffset(dateTime),
                    DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
                    string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateTimeOffset.")
                };
            }
        }
    }
}
