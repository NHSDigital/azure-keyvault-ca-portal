using Azure.Data.Tables;
using Azure.Identity;

namespace CaManager.Services
{
    public class TableAuditService : IAuditService
    {
        private readonly TableClient? _tableClient;
        private readonly ILogger<TableAuditService> _logger;

        public TableAuditService(IConfiguration configuration, ILogger<TableAuditService> logger)
        {
            _logger = logger;
            var tableName = configuration["Storage:AuditTableName"] ?? "auditlogs";
            var connectionString = configuration["Storage:ConnectionString"];
            var accountName = configuration["Storage:AccountName"];

            try
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    // Use connection string (e.g., Azurite or Access Key)
                    _tableClient = new TableClient(connectionString, tableName);
                }
                else if (!string.IsNullOrEmpty(accountName))
                {
                    // Use Managed Identity
                    var serviceUri = new Uri($"https://{accountName}.table.core.windows.net");
                    var credential = new DefaultAzureCredential();
                    _tableClient = new TableClient(serviceUri, tableName, credential);
                }
                else
                {
                    _logger.LogWarning("Storage:AccountName nor Storage:ConnectionString found in configuration. Audit logging disabled.");
                }

                if (_tableClient != null)
                {
                    _tableClient.CreateIfNotExists();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize TableAuditService.");
            }
        }

        public async Task LogAsync(string partitionKey, string operation, string details, string user, bool success = true)
        {
            if (_tableClient == null) return;

            try
            {
                // RowKey is ReverseTicks to enable efficient "newest first" querying
                var rowKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                
                var entity = new TableEntity(partitionKey, rowKey)
                {
                    { "Operation", operation },
                    { "Details", details },
                    { "User", user },
                    { "Success", success },
                    { "Timestamp", DateTime.UtcNow }
                };

                await _tableClient.AddEntityAsync(entity);
            }
            catch (Exception ex)
            {
                var msg = $"[TableAuditService] Failed to write to audit log: {ex.Message}";
                _logger.LogError(ex, msg);
                Console.Error.WriteLine(msg); // Ensure visibility in console
                // We typically don't want audit failures to crash the main flow, but in high-security apps we might.
                // For this portal, swallowing the error with a critical log is usually acceptable to keep the app running.
            }
        }
    }
}
