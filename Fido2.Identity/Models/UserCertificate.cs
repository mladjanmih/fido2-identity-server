using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Fido2IdentityServer.Identity.Models
{
    public class UserCertificate
    {
        public long Id { get; set; }
        public string Thumbprint { get; set; }
        public string Certificate { get; set; }
        public DateTime RegistrationDate{get; set;}
        public string Subject { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }

    }
}
