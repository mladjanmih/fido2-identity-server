using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Fido2IdentityServer.Identity.Models
{
    public class PaymentAuthorization
    {
        public long Id { get; set; }

        [ForeignKey("Payment")]
        public string PaymentId { get; set; }

        public virtual Payment Payment { get; set; }

        public string AuthenticatorData { get; set; }

        public int Type { get; set; }

        public string Signature { get; set; }

        public string ClientData { get; set; }

        public string PublicKeyId { get; set; }

        public DateTime AuthorizationDateTime { get; set; }
    }
}
