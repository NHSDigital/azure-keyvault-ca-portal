using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Keys;
using CaManager.Services;
using Moq;
using Xunit;

namespace CaManager.Tests
{
    public class KeyVaultSignatureGeneratorTests
    {
        [Fact]
        public void GetSignatureAlgorithmIdentifier_SHA256_ReturnsCorrectAsn1Sequence()
        {
            // Arrange
            var mockCrypto = new Mock<CryptographyClient>();
            using var cert = X509CertificateLoader.LoadCertificate(GetTestCertBytes()); 
            var generator = new KeyVaultSignatureGenerator(mockCrypto.Object, cert);

            // Act
            var result = generator.GetSignatureAlgorithmIdentifier(HashAlgorithmName.SHA256);

            // Assert
            // Expected: Sequence containing OID 1.2.840.113549.1.1.11 and NULL
            // 30 0D 06 09 2A 86 48 86 F7 0D 01 01 0B 05 00
            var expected = new byte[] { 
                0x30, 0x0D, 
                0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B, 
                0x05, 0x00 
            };
            
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SignData_UsingSHA256_CallsCryptoClientWithCorrectDigest()
        {
            // Arrange
            var mockCrypto = new Mock<CryptographyClient>();
            using var cert = X509CertificateLoader.LoadCertificate(GetTestCertBytes());
            var generator = new KeyVaultSignatureGenerator(mockCrypto.Object, cert);
            
            var data = new byte[] { 1, 2, 3, 4, 5 };
            // Compute expected hash
            using var sha = SHA256.Create();
            var expectedDigest = sha.ComputeHash(data);

            var expectedSignature = new byte[] { 0xAA, 0xBB };

            mockCrypto.Setup(c => c.SignAsync(SignatureAlgorithm.RS256, It.IsAny<byte[]>(), default))
                .ReturnsAsync(CreateSignResult(new KeyVaultKeyIdentifier(new Uri("https://kv.vault.azure.net/keys/k/v")), SignatureAlgorithm.RS256, expectedSignature));

            // Act
            var signature = generator.SignData(data, HashAlgorithmName.SHA256);

            // Assert
            Assert.Equal(expectedSignature, signature);
            mockCrypto.Verify(c => c.SignAsync(SignatureAlgorithm.RS256, It.Is<byte[]>(digest => digest.SequenceEqual(expectedDigest)), default), Times.Once);
        }

        private byte[] GetTestCertBytes()
        {
             // Create a self-signed cert on the fly for testing
             using var rsa = RSA.Create(2048);
             var req = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
             using var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
             return cert.Export(X509ContentType.Cert);
        }

        private static SignResult CreateSignResult(KeyVaultKeyIdentifier keyId, SignatureAlgorithm algorithm, byte[] signature)
        {
            var result = (SignResult)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SignResult));
            
            var type = typeof(SignResult);
            // Try setting properties (works even if read-only backing fields exist usually, but we need to find the backing field if it's strictly get-only without setter)
            // Actually, if it's a get-only property, SetValue might fail unless we find the backing field.
            
            SetField(result, "Signature", signature);

            
            SetField(result, "KeyId", "https://kv.vault.azure.net/keys/k/v");
            SetField(result, "Algorithm", algorithm);
            
            return result;
        }

        private static void SetField(object target, string propertyName, object value)
        {
            var type = target.GetType();
            // Try to find backing field usually format <PropName>k__BackingField
            var field = type.GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }
            
            // Try finding private field with lowercase or underscore
            field = type.GetField($"_{char.ToLower(propertyName[0])}{propertyName.Substring(1)}", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
             if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            // Fallback: try to set property if it has a setter (maybe private)
            var prop = type.GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
            {
               prop.SetValue(target, value);
               return;
            }
            
             var allFields = string.Join(", ", type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).Select(f => f.Name));
             throw new InvalidOperationException($"Could not set property {propertyName} on {type.Name}. Available fields: {allFields}");
        }
    }
}
