using Azure.Data.Tables;
using System;

var tableName = "auditlogsdev";
var connectionString = "UseDevelopmentStorage=true";

Console.WriteLine($"Querying table '{tableName}' in Azurite...");

try
{
    var client = new TableClient(connectionString, tableName);
    await client.CreateIfNotExistsAsync();

    var query = client.QueryAsync<TableEntity>();

    Console.WriteLine("Audit Log Entries:");
    Console.WriteLine("--------------------------------------------------");

    int count = 0;
    await foreach (var entity in query)
    {
        count++;
        var timestamp = entity.GetDateTimeOffset("Timestamp").GetValueOrDefault();
        var operation = entity.GetString("Operation");
        var details = entity.GetString("Details");
        var user = entity.GetString("User");
        
        Console.WriteLine($"[{timestamp}] {operation} by {user}: {details}");
    }

    if (count == 0)
    {
        Console.WriteLine("No logs found.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
