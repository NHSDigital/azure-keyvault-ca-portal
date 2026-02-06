using CaManager.Controllers;
using CaManager.Models;
using CaManager.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CaManager.Tests
{
    public class CertificatesControllerTests
    {
        [Fact]
        public async Task Index_ReturnsViewWithCertificates()
        {
            // Arrange
            var mockService = new Mock<IKeyVaultService>();
            mockService.Setup(s => s.GetCertificatesAsync())
                .ReturnsAsync(new List<Azure.Security.KeyVault.Certificates.KeyVaultCertificateWithPolicy>());
            
            var controller = new CertificatesController(mockService.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<List<Azure.Security.KeyVault.Certificates.KeyVaultCertificateWithPolicy>>(viewResult.Model);
        }

        [Fact]
        public async Task Create_ValidModel_CallsServiceAndRedirects()
        {
            // Arrange
            var mockService = new Mock<IKeyVaultService>();
            var controller = new CertificatesController(mockService.Object);
            var model = new CreateRootCaModel { SubjectName = "CN=Test", ValidityMonths = 12, KeySize = 2048 };

            // Act
            var result = await controller.Create(model);

            // Assert
            mockService.Verify(s => s.CreateRootCaAsync("CN=Test", 12, 2048), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsView()
        {
             // Arrange
            var mockService = new Mock<IKeyVaultService>();
            var controller = new CertificatesController(mockService.Object);
            controller.ModelState.AddModelError("Error", "Model is invalid");
            var model = new CreateRootCaModel();

            // Act
            var result = await controller.Create(model);

            // Assert
            mockService.Verify(s => s.CreateRootCaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            Assert.IsType<ViewResult>(result);
        }
    }
}
