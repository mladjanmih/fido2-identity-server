using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Fido
{
    [DataContract(Name="payment-ditigal-signature")]
    [Serializable]
    public class PaymentDigitalSignatureViewModel
    {
        [DataMember(Name = "paymentId")]
        public string PaymentId { get; set; }

        [DataMember(Name = "username")]
        public string UserId { get; set; }

        public string CreditorName { get; set; }

        public string DebtorName { get; set; }

        public string CreditorAccount { get; set; }

        public string DebtorAccount { get; set; }

        public decimal Amount { get; set; }
        //[JsonIgnore]
        //public string Challenge { get; set; }

        public bool Fido2Registered { get; set; }

        public bool SmartCardRegistered { get; set; }
    }
}
