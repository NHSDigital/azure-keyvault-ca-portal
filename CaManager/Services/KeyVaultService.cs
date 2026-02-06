using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;

namespace CaManager.Services
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly CertificateClient _certificateClient;
        private readonly KeyClient _keyClient;
        private readonly IConfiguration _configuration;

        public KeyVaultService(IConfiguration configuration)
        {
            _configuration = configuration;
            var kvUrl = _configuration["KeyVault:Url"];
            // Using DefaultAzureCredential - supports Managed Identity, CLI, VS, etc.
            var credential = new DefaultAzureCredential();

            if (string.IsNullOrEmpty(kvUrl)) throw new ArgumentNullException("KeyVault:Url is missing in configuration");

            _certificateClient = new CertificateClient(new Uri(kvUrl), credential);
            _keyClient = new KeyClient(new Uri(kvUrl), credential);
        }

        public async Task<List<KeyVaultCertificateWithPolicy>> GetCertificatesAsync()
        {
            var certs = new List<KeyVaultCertificateWithPolicy>();
            // Loop through all certificates
            await foreach (var certProp in _certificateClient.GetPropertiesOfCertificatesAsync())
            {
                try 
                {
                    // Get the full certificate policy and details
                    var cert = await _certificateClient.GetCertificateAsync(certProp.Name);
                    certs.Add(cert.Value);
                }
                catch
                {
                    // Ignore inaccessible certs or errors fetching details
                }
            }
            return certs;
        }
        
        public async Task<KeyVaultCertificateWithPolicy> GetCertificateAsync(string name)
        {
             return await _certificateClient.GetCertificateAsync(name);
        }

        public async Task<KeyVaultCertificateWithPolicy> CreateRootCaAsync(string subjectName, int validityMonths, int keySize)
        {
            var policy = new CertificatePolicy(WellKnownIssuerNames.Self, subjectName)
            {
                KeyType = CertificateKeyType.Rsa,
                KeySize = keySize,
                ContentType = CertificateContentType.Pem,
                Exportable = true, // Key can be exported (required for some backup scenarios, but insecure for strict CAs). 
                ValidityInMonths = validityMonths,
            };
            
            // Key Usage for a CA
            policy.KeyUsage.Add(CertificateKeyUsage.KeyCertSign);
            policy.KeyUsage.Add(CertificateKeyUsage.CrlSign);
            policy.KeyUsage.Add(CertificateKeyUsage.DigitalSignature);

            // Note: Setting BasicConstraints (CA=true) specifically via Policy is complex in the simple SDK model 
            // without custom JSON or specific extension helpers which might be limited.
            // However, Self-signed certs created this way usually default to standard profiles.
            // We will proceed with this default policy.

            var op = await _certificateClient.StartCreateCertificateAsync($"root-{Guid.NewGuid().ToString().Substring(0, 8)}", policy);
            return await op.WaitForCompletionAsync();
        }

        public async Task<KeyVaultCertificateWithPolicy> ImportRootCaAsync(string certificateName, byte[] pfxBytes, string? password)
        {
            var importOptions = new ImportCertificateOptions(certificateName, pfxBytes)
            {
                Password = password,
                Policy = new CertificatePolicy(WellKnownIssuerNames.Self, "CN=Imported Root")
            };
            return await _certificateClient.ImportCertificateAsync(importOptions);
        }

        public async Task<X509Certificate2> SignCsrAsync(string issuerCertName, byte[] csrBytes, int validityMonths)
        {
            // 1. Get Issuer Cert and Key ID
            var issuerCertBundle = await _certificateClient.GetCertificateAsync(issuerCertName);
            // We need the X509Certificate2 with the PRIVATE KEY? No, we are remote signing.
            // We just need the Public part to represent the issuer.
            var issuerCert = new X509Certificate2(issuerCertBundle.Value.Cer);
            
            // 2. Initialise Crypto Client for Remote Signing
            // keyId is the full identifier of the key backing the cert
            var keyId = issuerCertBundle.Value.KeyId; 
            // We explicitly use the Key Client for the specific key name if possible, or just the main client?
            // GetCryptographyClient(keyName, keyVersion) is better if we want to be specific.
            // KeyId format: https://vault.vault.azure.net/keys/keyname/version
            // We can parse it or just use the helper.
            
            // Note: _keyClient.GetCryptographyClient(name) creates a client for the latest key.
            // We should use the specific version bound to the cert.
            // issuerCertBundle.Value.KeyId is the URI.
            var cryptoClient = new CryptographyClient(issuerCertBundle.Value.KeyId, new DefaultAzureCredential());

            // 3. Load CSR
            var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

            // 4. Create Generator
            var generator = new KeyVaultSignatureGenerator(cryptoClient, issuerCert);

            // 5. Generate Serial Number
            var serialNumber = new byte[20];
            RandomNumberGenerator.Fill(serialNumber);

            // 6. Create Certificate
            var notBefore = DateTimeOffset.UtcNow;
            var notAfter = notBefore.AddMonths(validityMonths);

            var signedCert = csr.Create(
                issuerCert.SubjectName, // Issuer Name from KV Cert
                generator, 
                notBefore, 
                notAfter, 
                serialNumber);

            return signedCert;
        }
    }
}
