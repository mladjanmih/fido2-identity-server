using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Fido2IdentityServer.Identity.Models
{
    public class FidoLogin
    {
        [Key]
        public long Id { get; set; }

        public string UserId { get; set;
        }
        public virtual User User { get; set; }

        public byte[] PublicKeyId { get; set; }

        public byte[] PublicKey { get; set; }

        public byte[] UserHandle { get; set; }

        public uint SignatureCounter { get; set; }

        public string CredType { get; set; }

        public string AuthenticatorName { get; set; }

        public DateTime RegistrationDate { get; set; }

        public string AaGuid { get; set; }
    }
}
