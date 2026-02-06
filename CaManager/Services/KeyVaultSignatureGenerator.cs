using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Formats.Asn1;
using Azure.Security.KeyVault.Keys.Cryptography;

namespace CaManager.Services
{
    public class KeyVaultSignatureGenerator : X509SignatureGenerator
    {
        private readonly CryptographyClient _cryptoClient;
        private readonly X509Certificate2 _issuerCertificate;

        public KeyVaultSignatureGenerator(CryptographyClient cryptoClient, X509Certificate2 issuerCertificate)
        {
            _cryptoClient = cryptoClient;
            _issuerCertificate = issuerCertificate;
        }

        protected override PublicKey BuildPublicKey()
        {
            // Return Issuer's PublicKey
            return _issuerCertificate.PublicKey;
        }

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            // OID for sha256WithRSAEncryption: 1.2.840.113549.1.1.11
            // This method expects the full AlgorithmIdentifier ASN.1 sequence, not just the OID.
            // However, X509SignatureGenerator usage often varies. The .NET implementation for RSAPKCS1SignatureGenerator
            // returns the AlgorithmIdentifier sequence containing the OID and NULL parameters.
            
            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                var writer = new AsnWriter(AsnEncodingRules.DER);
                writer.PushSequence();
                writer.WriteObjectIdentifier("1.2.840.113549.1.1.11"); // sha256WithRSAEncryption
                writer.WriteNull();
                writer.PopSequence();
                return writer.Encode();
            }
            throw new NotSupportedException($"Hash algorithm {hashAlgorithm} is not supported. Only SHA256 is implemented.");
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm != HashAlgorithmName.SHA256)
            {
                throw new NotSupportedException($"Hash algorithm {hashAlgorithm} is not supported. Only SHA256 is implemented.");
            }

            // Remote Sign using Key Vault
            // Hash the data first, then Sign.
            using var hasher = SHA256.Create();
            var digest = hasher.ComputeHash(data);
            
            var result = _cryptoClient.SignAsync(SignatureAlgorithm.RS256, digest).GetAwaiter().GetResult();
            return result.Signature;
        }
    }
}
