using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DriveSafe.SqlPerfTest
{
    public static class RecUtils
    {
        public static async IAsyncEnumerable<T> QueryRecords<T>(
            string connStr,
            string query,
            Func<SqlDataReader, T> map,
            Action<SqlCommand>? bind = null)
        {
            await using var conn = new SqlConnection(connStr);

            await conn.OpenAsync();
           
            await foreach(var rec in QueryRecords(conn, query, map, bind))
                yield return rec;

            await conn.CloseAsync();
        }

        public static async IAsyncEnumerable<T> QueryRecords<T>(
            SqlConnection conn,
            string query,
            Func<SqlDataReader, T> map,
            Action<SqlCommand>? bind = null!)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            bind?.Invoke(cmd);

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) yield return map(rdr);
        }

        public static DateTimeOffset? GetNullableDateTimeOffset(this SqlDataReader rdr, int idx) => rdr.IsDBNull(idx) ? null : rdr.GetDateTimeOffset(idx);
        public static double? GetNullableDouble(this IDataReader rdr, int idx) => rdr.IsDBNull(idx) ? null : rdr.GetDouble(idx);
        public static short? GetNullableShort(this IDataReader rdr, int idx) => rdr.IsDBNull(idx) ? null : rdr.GetInt16(idx);

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            var list = new List<T>();
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                list.Add(item);

            return list;
        }

        public static async Task<Dictionary<K, T>> ToDictionaryAsync<K, T>(this IAsyncEnumerable<T> source, Func<T, K> keySelector, IEqualityComparer<K>? comparer = default, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<K, T>(comparer);
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                dict.Add(keySelector(item), item);

            return dict;
        }

        public static Task AddRecords<T>(string connStr, Expression<Func<T, int>> keyAccessor, IReadOnlyList<T> recs)
        {
            var lambda = keyAccessor as LambdaExpression;
            var memberExpr = lambda.Body as MemberExpression;

            var keyName = memberExpr?.Member.Name ?? throw new ArithmeticException("keyAccessor not a member expression");

            return AddRecords(connStr, typeof(T).Name, keyName, typeof(T).GetProperties().Select(x => x.Name).Where(x => x != keyName).ToArray(), recs);
        }

        public static Task AddRecords<T>(string connStr, string keyName, IReadOnlyList<T> recs) =>
            AddRecords(connStr, typeof(T).Name, keyName, typeof(T).GetProperties().Select(x => x.Name).Where(x => x != keyName).ToArray(), recs);

        public static async Task AddRecords<T>(string connStr, string table, string keyName, string[] propNames, IReadOnlyList<T> recs)
        {
            var conn = new SqlConnection(connStr);

            await conn.OpenAsync().ConfigureAwait(false);

            var paramAccessors = typeof(T).GetProperties().ToDictionary(x => x.Name, x => x.GetMethod);
            var keySetter = typeof(T).GetProperty(keyName)?.SetMethod;

            foreach (var rec in recs)
            {
                var insCmd = conn.CreateCommand(); 
                insCmd.CommandText = $"insert into {table}({string.Join(", ", propNames)}) values ({string.Join(", ", propNames.Select(c => "@" + c))})";

                foreach (var colName in propNames)
                    insCmd.Parameters.AddWithValue("@" + colName, paramAccessors[colName]?.Invoke(rec, null));

                await insCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (keySetter != null)
                {
                    var idCmd = conn.CreateCommand();
                    idCmd.CommandText = "select @@identity";
                    var id = Convert.ToInt32(await idCmd.ExecuteScalarAsync().ConfigureAwait(false));
                    keySetter.Invoke(rec, new object[] { id });
                }
            }

            await conn.CloseAsync().ConfigureAwait(false);
        }
    }
}
