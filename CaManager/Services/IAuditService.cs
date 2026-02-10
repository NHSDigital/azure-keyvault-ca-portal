namespace CaManager.Services
{
    public interface IAuditService
    {
        Task LogAsync(string partitionKey, string operation, string details, string user, bool success = true);
    }
}
