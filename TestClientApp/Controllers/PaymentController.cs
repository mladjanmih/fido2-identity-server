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
                UserId = sub
            });

            //var result = Redirect($"{_configuration["Identity"]}/Fido/PaymentDigitalSignature?paymentId={paymentId}&returnUrl=https://localhost:44342/Payment/PaymentSigningCallback");
            //return result;
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

        public IActionResult PaymentSigningCallback(string paymentId)
        {
            var payment = _paymentStore.GetPayment(paymentId);
            if (payment == null)
            {
                var vm = BuildPaymentsViewModel();
                vm.SigningSuccess = false;
                return View("Index", vm);
            }

            var pk = _authenticaationContext.FidoLogins.First(x => x.PublicKeyId == payment.PublicKeyId);

            byte[] hashedClientDataJson;
            using (var sha = SHA256.Create())
            {
                hashedClientDataJson = sha.ComputeHash(Fido2NetLib.Base64Url.Decode(payment.ClientData));
            }

            var authenticatorData = Fido2NetLib.Base64Url.Decode(payment.AuthenticatorData);
            byte[] data = new byte[authenticatorData.Length + hashedClientDataJson.Length];
            Buffer.BlockCopy(authenticatorData, 0, data, 0, authenticatorData.Length);
            Buffer.BlockCopy(hashedClientDataJson, 0, data, authenticatorData.Length, hashedClientDataJson.Length);
            var cpk = new Fido2NetLib.Objects.CredentialPublicKey(pk.PublicKey);
            if (true != cpk.Verify(data, Fido2NetLib.Base64Url.Decode(payment.Signature)))
            {
                var vm = BuildPaymentsViewModel();
                vm.SigningSuccess = false;
                return View("Index", vm);
            }
            var retvm = BuildPaymentsViewModel();
            retvm.SigningSuccess = true;
            return View("Index", retvm);
        }

        private PaymentsViewModel BuildPaymentsViewModel()
        {
            var sub = User.Claims.First(x => x.Type == "sub").Value;
            var payments = _paymentStore.GetUserPayments(sub);
            var vm = new PaymentsViewModel();
            foreach (var p in payments)
            {
                vm.Payments.Add(new PaymentViewModel()
                {
                    CreditorAccount = p.CreditorAccount,
                    RequestDateTime = p.RequestDateTime,
                    Amount = p.Amount,
                    CreditorName = p.CreditorName,
                    DebtorAccount = p.DebtorAccount,
                    DebtorName = p.DebtorName,
                    PaymentId = p.Id,
                    HasSignature = p.HasSignature
                });
            }


            return vm;
        }
    }
}