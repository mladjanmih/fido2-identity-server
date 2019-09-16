using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Certificate
{
    public class CertificatesViewModel
    {
        public List<CertificateViewModel> Certificates { get; set; } = new List<CertificateViewModel>();
        public bool? RegistrationSuccess { get; set; }
    }

    public class CertificateViewModel
    {
        public string Thumbprint { get; set; }

        public string Subject { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
