using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestClientApp.Models
{
    public class PaymentsViewModel
    {
        public List<PaymentViewModel> Payments { get; set; } = new List<PaymentViewModel>();

        public bool? SigningSuccess { get; set; }
    }


    public class PaymentViewModel
    {
        public string PaymentId { get; set; }

        public string CreditorAccount { get; set; }

        public string CreditorName { get; set; }

        public string DebtorAccount { get; set; }

        public string DebtorName { get; set; }

        public decimal Amount { get; set; }
        
        public DateTime RequestDateTime { get; set; }
        
        public bool HasSignature { get; set; }
    }
}
