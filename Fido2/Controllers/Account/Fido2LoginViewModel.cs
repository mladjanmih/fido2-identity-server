using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Account
{
    public class Fido2LoginViewModel
    {
        public List<string> AuthenticatorTypes { get; set; } = new List<string>();

        public string ReturnUrl { get; set; }

        public bool RememberLogin { get; set; }
    }
}
