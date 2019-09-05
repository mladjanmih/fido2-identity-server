using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Fido
{
    public class DevicesViewModel
    {
        public bool? RegistrationSuccess { get; set; }

        public List<FidoLoginViewModel> FidoLogins { get; set; } = new List<FidoLoginViewModel>();
    }

    public class FidoLoginViewModel
    {
        public string CredentialType { get; set; }
        
        public DateTime RegistrationDate { get; set; }

        public string AaGuid { get; set; }

        public string CredentialId { get; set; }
    }
}
