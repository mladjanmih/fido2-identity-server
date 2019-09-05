using Fido2NetLib;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Account
{
    public class WebauthnAssertionViewModel
    {

        public IHtmlContent AssertionOptions { get; set; }

        public string ReturnUrl { get;  set; }

        public string RememberLogin { get; set; }
    }
}
