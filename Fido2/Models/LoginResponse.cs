using Fido2NetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Models
{
    [DataContract(Name="login-response")]
    [Serializable]
    public class LoginResponse
    {
        [DataMember(Name = "fidoLogin")]
        public bool FidoLogin { get; set; }

        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name ="returnUrl")]
        public string ReturnUrl { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "makeAssertionOptions")]
        public AssertionOptions Options { get; set; }

    }
}
