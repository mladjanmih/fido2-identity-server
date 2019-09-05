using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Identity.Models
{
    public class CredentialPublicKey
    {
        public string KeyType { get; set; }

        public string Algorithm { get; set; }

        public string Curve { get; set; }

        public string X { get; set; }

        public string Y { get; set; }
    }
}
