using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Fido2IdentityServer.Identity;
using Fido2IdentityServer.Identity.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PeterO.Cbor;

namespace Fido2IdentityServer.Controllers.Fido
{
    public class FidoController : Controller
    {
        private readonly UserManager<User> _users;
        private readonly AuthenticationContext _authContext;
        private Fido2 _lib;
        private readonly string _origin;
        private IMetadataService _mds;

        public FidoController(
            UserManager<User> users,
            AuthenticationContext authContext,
            IConfiguration configuraiton)
        {
            
           
            _users = users;
            _authContext = authContext;

            var invalidToken = "6d6b44d78b09fed0c5559e34c71db291d0d322d4d4de0000";
            _origin = configuraiton["Fido2:Origin"];
            var MDSAccessKey = configuraiton["fido2:MDSAccessKey"];
            var MDSCacheDirPath = configuraiton["fido2:MDSCacheDirPath"] ?? Path.Combine(Path.GetTempPath(), "fido2mdscache");
            _mds = string.IsNullOrEmpty(MDSAccessKey) ? null : MDSMetadata.Instance(MDSAccessKey, MDSCacheDirPath);
            if (null != _mds)
            {
                if (false == _mds.IsInitialized())
                    _mds.Initialize().Wait();
            }

            _lib = new Fido2(new Fido2Configuration()
            {
                ServerDomain = configuraiton["Fido2:ServerDomain"],
                ServerName = "Fido2 Identity Server",
                Origin = _origin,
                MetadataService = _mds 
            });
        }

        private DevicesViewModel BuildDevicesViewModel(User user)
        {
            var vm = new DevicesViewModel();
            var devices = _authContext.FidoLogins.Where(l => l.UserId == user.Id);
            foreach (var device in devices)
            {
                vm.FidoLogins.Add(new FidoLoginViewModel()
                {
                    AaGuid = device.AaGuid,
                    CredentialType = device.CredType,
                    RegistrationDate = device.RegistrationDate,
                    CredentialId = device.PublicKeyId != null && device.PublicKeyId.Length > 0 ? System.Convert.ToBase64String(device.PublicKeyId) : string.Empty
                });
            }

            return vm;
        }
        public async Task<IActionResult> Devices()
        {
            var user = await GetUserAsync();
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var vm = BuildDevicesViewModel(user);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> FidoRegister(string button)
        {
            var sub = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                return RedirectToAction("Index", "Home");
            }
            var user = await _users.FindByIdAsync(sub);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var existingDbKeys = _authContext.FidoLogins.Where(x => x.UserId == user.Id).Select(x => x.PublicKeyId);
            var existingKeys = new List<PublicKeyCredentialDescriptor>();
            foreach (var key in existingDbKeys)
            {
                existingKeys.Add(new PublicKeyCredentialDescriptor(key));
            }

            //OVDE SI PRESKOCIO EKSTENZIJE
            var exts = new AuthenticationExtensionsClientInputs() { Extensions = true, UserVerificationIndex = true, Location = true, UserVerificationMethod = true, BiometricAuthenticatorPerformanceBounds = new AuthenticatorBiometricPerfBounds { FAR = float.MaxValue, FRR = float.MaxValue } };
            Fido2User f2User = new Fido2User()
            {
                DisplayName = user.DisplayName,
                Id = System.Text.Encoding.ASCII.GetBytes(user.Id),
                Name = user.UserName
            };
            var authSelect = new AuthenticatorSelection()
            {
             
                RequireResidentKey = false,
                UserVerification = UserVerificationRequirement.Preferred
            };

            List<PubKeyCredParam> pubKeyCredParams = null;
            switch(button)
            {
                case "yubikey":
                    authSelect.AuthenticatorAttachment = AuthenticatorAttachment.CrossPlatform;
                    pubKeyCredParams = new List<PubKeyCredParam>()
                    {
                        new PubKeyCredParam
                        {
                            Type = PublicKeyCredentialType.PublicKey,
                            Alg = -7
                        }
                    };
                    HttpContext.Session.SetString("fido2.attestationOptions.authenticatorType", "yubikey");
                    break;
                case "windows-hello":
                    authSelect.AuthenticatorAttachment = AuthenticatorAttachment.Platform;
                    pubKeyCredParams = new List<PubKeyCredParam>()
                    {
                        new PubKeyCredParam
                        {
                            Type = PublicKeyCredentialType.PublicKey,
                            Alg = -257
                        }
                    };
                    HttpContext.Session.SetString("fido2.attestationOptions.authenticatorType", "windows-hello");
                    break;
            }

            var options = _lib.RequestNewCredential(f2User, existingKeys, authSelect, AttestationConveyancePreference.Direct, exts);
            if (pubKeyCredParams != null)
            {
                options.PubKeyCredParams = pubKeyCredParams;
            }

            HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());
            var challenge = CryptoRandom.CreateRandomKeyString(16);
            return View(new RegisterViewModel() { Challenge = challenge, CredentialCreateOptions = options });
            //            return View(new RegisterViewModel { Challenge = challenge, RelyingPartyId = RelyingPartyId, Username = user.UserName });
        }

        [HttpPost]
        public async Task<IActionResult> RegisterCallback([FromBody] AuthenticatorAttestationRawResponse model)
        {
            var sub = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                return RedirectToAction("Index", "Home");
            }
            var user = await _users.FindByIdAsync(sub);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // 1. get the options we sent the client
                var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
                var options = CredentialCreateOptions.FromJson(jsonOptions);
                var authenticatorName = HttpContext.Session.GetString("fido2.attestationOptions.authenticatorType");
                // 2. Create callback so that lib can verify credential id is unique to this user
                IsCredentialIdUniqueToUserAsyncDelegate callback = async (IsCredentialIdUniqueToUserParams args) =>
                {
                    var users = _authContext.FidoLogins.Where( l => l.PublicKeyId.SequenceEqual(args.CredentialId));
                    if (users.Count() > 0) return false;

                    return true;
                };

                // 2. Verify and make the credentials
                var success = await _lib.MakeNewCredentialAsync(model, options, callback);
                var dbUser = _authContext.Users.First(x => x.Id == user.Id);
                dbUser.TwoFactorEnabled = true;
                var login = new FidoLogin()
                {
                    PublicKeyId = success.Result.CredentialId,
                    AaGuid = success.Result.Aaguid.ToString(),
                    PublicKey = success.Result.PublicKey,
                    SignatureCounter = success.Result.Counter,
                    CredType = success.Result.CredType,
                    RegistrationDate = DateTime.Now,
                    User = dbUser,
                    UserHandle = success.Result.User.Id,
                    AuthenticatorName = authenticatorName
                };
                _authContext.FidoLogins.Add(login);
                _authContext.SaveChanges();


                // 4. return "ok" to the client
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = true }); 
            }
        }

        public async Task<IActionResult> RegisterSuccess()
        {
            var user = await GetUserAsync();
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var vm = BuildDevicesViewModel(user);
            vm.RegistrationSuccess = true;
            return View("Devices", vm);
        }
        public async Task<IActionResult> RegisterFailed ()
        {
            var user = await GetUserAsync();
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var vm = BuildDevicesViewModel(user);
            vm.RegistrationSuccess = false;
            return View("Devices", vm);
        }

        private async Task<User> GetUserAsync()
        {
            var sub = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                return null;
            }
            var user = await _users.FindByIdAsync(sub);
            if (user == null)
            {
                return null;
            }

            return user;
        }
    }
}