using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TestClientApp.Models
{
    public class PaymentInitiateViewModel
    {
        [Required]
        public string CreditorAccount { get; set; }

        [Required]
        public string DebtorAccount { get; set; }

        [Required]
        public string CreditorName { get; set; }

        [Required]
        public string DebtorName { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}
