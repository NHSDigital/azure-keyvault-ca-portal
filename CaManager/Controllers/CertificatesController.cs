using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CaManager.Models;
using CaManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaManager.Controllers
{
    [Authorize]
    public class CertificatesController : Controller
    {
        private readonly IKeyVaultService _kvService;

        public CertificatesController(IKeyVaultService kvService)
        {
            _kvService = kvService;
        }

        public async Task<IActionResult> Index()
        {
            var certs = await _kvService.GetCertificatesAsync();
            return View(certs);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateRootCaModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRootCaModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _kvService.CreateRootCaAsync(model.SubjectName, model.ValidityMonths, model.KeySize);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(ImportRootCaModel model)
        {
             if (!ModelState.IsValid) return View(model);

             using var stream = new MemoryStream();
             await model.CertificateFile.CopyToAsync(stream);
             var bytes = stream.ToArray();

             try
             {
                 await _kvService.ImportRootCaAsync(model.Name, bytes, model.Password);
                 return RedirectToAction(nameof(Index));
             }
             catch (Exception ex)
             {
                 ModelState.AddModelError("", ex.Message);
                 return View(model);
             }
        }

        [HttpGet]
        public IActionResult Sign(string id)
        {
            // ID = Issuer Certificate Name
            ViewBag.IssuerName = id;
            return View();
        }

        [HttpPost]
        public IActionResult Inspect(string issuerName, IFormFile csrFile)
        {
            if (csrFile == null || csrFile.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a CSR.");
                return View("Sign", new { id = issuerName });
            }

            try 
            {
                using var stream = new MemoryStream();
                csrFile.CopyTo(stream);
                var bytes = stream.ToArray();
                
                // Attempt to parse CSR
                // PEM or DER? LoadSigningRequest handles both usually if using generic Load or we need to detect.
                // CertificateRequest.LoadSigningRequest accepts byte[]
                
                var csr = CertificateRequest.LoadSigningRequest(bytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);
                
                var model = new InspectCsrModel
                {
                    IssuerName = issuerName,
                    SubjectDn = csr.SubjectName.Name,
                    PublicKeyInfo = csr.PublicKey.Oid.FriendlyName, // e.g. RSA
                    SignatureAlgorithm = csr.HashAlgorithm.Name,
                    CsrContentBase64 = Convert.ToBase64String(bytes),
                    ValidityMonths = 12
                };

                return View("Inspect", model);
            }
            catch (Exception ex) 
            {
                 ModelState.AddModelError("", $"Error parsing CSR: {ex.Message}");
                 ViewBag.IssuerName = issuerName;
                 return View("Sign");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteSign(InspectCsrModel model)
        {
             try
             {
                 var csrBytes = Convert.FromBase64String(model.CsrContentBase64);
                 var cert = await _kvService.SignCsrAsync(model.IssuerName, csrBytes, model.ValidityMonths);

                 var certBytes = cert.Export(X509ContentType.Cert);
                 return File(certBytes, "application/x-x509-ca-cert", "signed-certificate.cer");
             }
             catch (Exception ex)
             {
                 ModelState.AddModelError("", ex.Message);
                 return View("Inspect", model);
             }
        }
    }
}
