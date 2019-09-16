using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Models
{
    [DataContract(Name = "smart-card-options")]
    [Serializable]
    public class SmartCardOptions
    {
        [DataMember(Name = "payload")]
        public string Payload { get; set; }
    }
}
