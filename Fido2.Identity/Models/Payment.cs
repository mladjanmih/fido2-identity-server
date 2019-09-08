using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Identity.Models
{
    public class Payment
    {
        public string UserId { get; set; }


        public string Id { get; set; }
     
        public string CreditorAccount { get; set; }


        public string DebtorAccount { get; set; }


        public string CreditorName { get; set; }


        public string DebtorName { get; set; }

        public decimal Amount { get; set; }

        public bool HasSignature { get; set; }

        public string Signature { get; set; }

        public string PublicKeyId { get; set; }

        public string AuthenticatorData { get; set; }

        public string ClientData { get; set; }

        public DateTime RequestDateTime { get; set; }
    }
}
