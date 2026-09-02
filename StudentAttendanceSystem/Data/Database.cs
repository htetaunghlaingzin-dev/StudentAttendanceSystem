using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace StudentAttendanceSystem.Data;

public sealed class Database
{
    public string ConnectionString { get; }

    public Database()
    {
        var config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false).Build();
        ConnectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing.");
    }

    public SqlConnection CreateConnection() => new(ConnectionString);

    public async Task<T> QuerySingleAsync<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("The requested record was not found.");
        return map(reader);
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
    {
        var items = new List<T>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) items.Add(map(reader));
        return items;
    }

    public async Task<int> ExecuteAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return await command.ExecuteNonQueryAsync();
    }
}
