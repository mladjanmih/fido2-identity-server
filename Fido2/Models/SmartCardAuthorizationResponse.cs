using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Models
{
    [DataContract(Name="smart-card-authorization-response")]
    [Serializable]
    public class SmartCardAuthorizationResponse
    {
        [DataMember(Name = "token")]
        public string Token { get; set; }

        [DataMember(Name = "certificate")]
        public string Certificate { get; set; }

        [DataMember(Name = "algorithm")]
        public string Algorithm { get; set; }
    }
}
