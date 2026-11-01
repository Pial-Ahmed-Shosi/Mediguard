// Throwaway dev helper: runs a SQL file against the MediGuard PostgreSQL database.
// Usage: dotnet run --project Tools/DbSql -- <path-to.sql>
// Connection string comes from the DG_CONN environment variable.
using Npgsql;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: DbSql <sql-file>");
    return 1;
}

var connString = Environment.GetEnvironmentVariable("DG_CONN")
    ?? throw new InvalidOperationException("DG_CONN environment variable is not set.");

var sql = File.ReadAllText(args[0]);

await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

// Statements are accumulated line by line and executed when a line ends with ";".
int pos = 0;
var buffer = new System.Text.StringBuilder();
foreach (var line in sql.Split('\n'))
{
    var trimmed = line.Trim();
    if (trimmed.StartsWith("--") || trimmed.Length == 0)
    {
        if (buffer.Length == 0) continue;
    }
    buffer.AppendLine(line);
    if (trimmed.EndsWith(";"))
    {
        var statement = buffer.ToString();
        buffer.Clear();
        await using var cmd = new NpgsqlCommand(statement, conn);
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                var cols = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++) cols.Add(reader.GetName(i));
                Console.WriteLine(string.Join(" | ", cols));
                while (await reader.ReadAsync())
                {
                    var vals = new List<string>();
                    for (var i = 0; i < reader.FieldCount; i++)
                        vals.Add(reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "");
                    Console.WriteLine(string.Join(" | ", vals));
                }
            }
            var more = await reader.NextResultAsync();
            while (more) { more = await reader.NextResultAsync(); }
            Console.WriteLine("-- statement ok");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            return 2;
        }
        pos++;
    }
}
Console.WriteLine($"Done. Executed {pos} statement(s).");
return 0;
