using System.ComponentModel.DataAnnotations;

namespace CaManager.Models
{
    public class CreateRootCaModel
    {
        [Required]
        [Display(Name = "Subject DN (e.g. CN=MyRootCA)")]
        public string SubjectName { get; set; } = "CN=MyRootCA";

        [Required]
        [Display(Name = "Validity (Months)")]
        public int ValidityMonths { get; set; } = 60;

        [Required]
        [Display(Name = "Key Size")]
        public int KeySize { get; set; } = 4096;
    }

    public class ImportRootCaModel
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public IFormFile CertificateFile { get; set; }
        
        public string? Password { get; set; }
    }

    public class InspectCsrModel
    {
        public string IssuerName { get; set; }
        
        public string SubjectDn { get; set; }
        public string PublicKeyInfo { get; set; }
        public string SignatureAlgorithm { get; set; }
        
        [Required]
        public string CsrContentBase64 { get; set; } // Pass between steps
        
        [Required]
        [Display(Name = "Validity for New Cert (Months)")]
        public int ValidityMonths { get; set; } = 12;
    }
}
