using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;

namespace CaManager.Services
{
    public interface IKeyVaultService
    {
        Task<List<KeyVaultCertificateWithPolicy>> GetCertificatesAsync();
        Task<KeyVaultCertificateWithPolicy> GetCertificateAsync(string name);
        Task<KeyVaultCertificateWithPolicy> CreateRootCaAsync(string subjectName, int validityMonths, int keySize);
        Task<KeyVaultCertificateWithPolicy> ImportRootCaAsync(string certificateName, byte[] pfxBytes, string? password);
        Task<X509Certificate2> SignCsrAsync(string issuerCertName, byte[] csrBytes, int validityMonths);
    }
}
