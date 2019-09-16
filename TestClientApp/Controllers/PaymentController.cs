using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Fido2IdentityServer.Identity;
using Fido2IdentityServer.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TestClientApp.Models;
using TestClientApp.Stores;

namespace TestClientApp.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly PaymentStore _paymentStore;
        private readonly IConfiguration _configuration;
        private readonly AuthenticationContext _authenticaationContext;
        public PaymentController(
            PaymentStore paymentStore,
            IConfiguration configuration,
            AuthenticationContext authenticationContext)
        {
            _paymentStore = paymentStore;
            _configuration = configuration;
            _authenticaationContext = authenticationContext;
        }
        public IActionResult Index()
        {
            var vm = BuildPaymentsViewModel();
            return View("Index", vm);

        }

        [HttpGet] 
        public IActionResult StartPaymentInitiation()
        {
            return View("InitiatePayment");
        }

        [HttpPost]
        public IActionResult InitiatePayment(PaymentInitiateViewModel model)
        {

            var sub = User.Claims.First(x => x.Type == "sub").Value;
            var paymentId = _paymentStore.AddPayment(new Payment()
            {
                Amount = model.Amount,
                CreditorAccount = model.CreditorAccount,
                DebtorAccount = model.DebtorAccount,
                CreditorName = model.CreditorName,
                DebtorName = model.DebtorName,
                RequestDateTime = DateTime.Now,
                UserId = sub,
                Status = "received"
            });

            var vm = BuildPaymentsViewModel();
            return View("Index", vm);
        }

        public IActionResult SignPayment(string paymentId)
        {
            if (_paymentStore.Exists(paymentId))
            {
                var result = Redirect($"{_configuration["Identity"]}/Fido/PaymentDigitalSignature?paymentId={paymentId}&returnUrl=https://localhost:44342/Payment/PaymentSigningCallback");
                return result;
            }
            else
            {
                var vm = BuildPaymentsViewModel();
                return View("Index", vm);
            }
        }

        public IActionResult PaymentSigningCallback(bool success)
        {
            var vm = BuildPaymentsViewModel();
            vm.SigningSuccess = success;
            return View("Index", vm);
        }

        private PaymentsViewModel BuildPaymentsViewModel()
        {
            var sub = User.Claims.First(x => x.Type == "sub").Value;
            var payments = _paymentStore.GetUserPayments(sub);
            var vm = new PaymentsViewModel();
            foreach (var p in payments)
            {
                var authorization = p.PaymentAuthorizations.FirstOrDefault();
                vm.Payments.Add(new PaymentViewModel()
                {
                    CreditorAccount = p.CreditorAccount,
                    RequestDateTime = p.RequestDateTime,
                    Amount = p.Amount,
                    CreditorName = p.CreditorName,
                    DebtorAccount = p.DebtorAccount,
                    DebtorName = p.DebtorName,
                    PaymentId = p.Id,
                    HasSignature = p.PaymentAuthorizations.Any(),
                    AuthorizationDateTime = authorization?.AuthorizationDateTime
                });

            }


            return vm;
        }
    }
}