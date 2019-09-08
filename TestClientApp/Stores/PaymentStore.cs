using Fido2IdentityServer.Identity;
using Fido2IdentityServer.Identity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestClientApp.Models;

namespace TestClientApp.Stores
{
    public class PaymentStore
    {
        private readonly AuthenticationContext _context;
        public PaymentStore(AuthenticationContext context)
        {
            _context = context;
        }
        
        public List<Payment> GetUserPayments(string userId)
        {
            return _context.Payments.Where(x => x.UserId == userId).ToList();
        }

        public string AddPayment(Payment payment)
        {
            //string id = null;
            //lock (_payments)
            //{
            //    id = Guid.NewGuid().ToString();
            //    while (_payments.ContainsKey(id))
            //    {
            //        id = Guid.NewGuid().ToString();
            //    }

            //    payment.PaymentId = id;
            //    _payments.Add(id, payment);
            //}
            //return id;
            payment.Id = Guid.NewGuid().ToString();
            _context.Payments.Add(payment);
            _context.SaveChanges();
            return payment.Id;

        }

        public Payment GetPayment(string id)
        {
            //lock (_payments)
            //{
            //    if (_payments.ContainsKey(id))
            //        return _payments[id];
            //    else
            //        return null;
            //}
            var payment = _context.Payments.FirstOrDefault(x => x.Id == id);
            return payment;
        }

        public bool Exists(string id)
        {
            //lock (_payments)
            //{
            //    return _payments.ContainsKey(id);
            //}
            return _context.Payments.FirstOrDefault(x => x.Id == id) != null;
        }
    }
}
