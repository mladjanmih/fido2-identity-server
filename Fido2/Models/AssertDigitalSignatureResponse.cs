using Fido2NetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Models
{   
    [DataContract(Name = "assert-digital-signature")]
    [Serializable]
    public class AssertDigitalSignatureResponse
    {
        [DataMember(Name = "makeAssertionOptions")]
        public AssertionOptions AssertionOptions { get; set; }

        [DataMember(Name = "smartCardOptions")]
        public SmartCardOptions SmartCardOptions { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }
    }
}
