using Fido2NetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Account
{
    public class AssertionResult
    {
        public AuthenticatorAssertionRawResponse Assertion { get; set; }

        public string ReturnUrl { get; set; }

        public string RememberLogin { get; set; }
    }
}
