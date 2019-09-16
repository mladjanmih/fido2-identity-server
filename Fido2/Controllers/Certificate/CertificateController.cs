using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fido2IdentityServer.Identity;
using Fido2IdentityServer.Identity.Models;
using Fido2IdentityServer.Models;
using Fido2IdentityServer.Utilities;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fido2IdentityServer.Controllers.Certificate
{

    [Authorize]
    public class CertificateController : Controller
    {
        private readonly UserManager<User> _users;
        private readonly AuthenticationContext _authContext;

        public CertificateController(
            UserManager<User> users,
            AuthenticationContext authContext)
        {


            _users = users;
            _authContext = authContext;

        }
        public IActionResult Index()
        {
            var vm = BuildCertificateViewModel();
            return View(vm);
        }

        public IActionResult RegistrationFinished(bool success)
        {
            var vm = BuildCertificateViewModel();
            vm.RegistrationSuccess = success;
            return View("Index", vm);
        }

        public IActionResult RemoveCertificate(string certThumbprint)
        {
            var cert = _authContext.UserCertificates.FirstOrDefault(x => x.Thumbprint == certThumbprint);
            if (cert != null)
            {
                _authContext.UserCertificates.Remove(cert);
                _authContext.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetRegisterCertificateOptions()
        {
            var challenge = CryptoRandom.CreateRandomKeyString(30);
            HttpContext.Session.SetString("certificateRegister.challenge", challenge);
            return Json(new { challenge });
        }

        [HttpPost]
        public async Task<IActionResult> RegisterCertificateCallback([FromBody]SmartCardAuthorizationResponse smartCardAuthorizationResponse)
        {
            var sub = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                return Json(new { success = false });
            }

            var user = await _users.FindByIdAsync(sub);
            if (smartCardAuthorizationResponse == null || string.IsNullOrEmpty(smartCardAuthorizationResponse.Certificate) || string.IsNullOrEmpty(smartCardAuthorizationResponse.Token))
            {
                return Json(new { success = false });
            }

            var certificate = CertificateUtilities.GetAndValidateCertificate(smartCardAuthorizationResponse.Certificate);
            if (certificate == null)
            {
                return Json(new { success = false });
            }

            if (_authContext.UserCertificates.FirstOrDefault(x => x.Thumbprint == certificate.Thumbprint) != null)
            {
                return Json(new { success = false });
            }

            var payload = HttpContext.Session.GetString("certificateRegister.challenge");
            var verifyResult = JwtUtils.ValidateJWT(
              certificate,
              smartCardAuthorizationResponse.Token,
              smartCardAuthorizationResponse.Algorithm,
              payload);

            if (verifyResult)
            {
                var dbuser = _authContext.Users.First(u => u.Id == user.Id);
                var userCert = new UserCertificate()
                {
                    Certificate = Convert.ToBase64String(certificate.RawData),
                    Thumbprint = certificate.Thumbprint,
                    User = dbuser,
                    RegistrationDate = DateTime.Now,
                    Subject = certificate.Subject
                };
                _authContext.UserCertificates.Add(userCert);
                _authContext.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }


        private CertificatesViewModel BuildCertificateViewModel()
        {
            var vm = new CertificatesViewModel();
            var sub = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                return vm;
            }

            foreach(var certificate in _authContext.UserCertificates.Where(x => x.UserId == sub))
            {
                vm.Certificates.Add(new CertificateViewModel()
                {
                    RegistrationDate = certificate.RegistrationDate,
                    Subject = certificate.Subject,
                    Thumbprint = certificate.Thumbprint
                });
            }

            return vm;
        }
    }
}