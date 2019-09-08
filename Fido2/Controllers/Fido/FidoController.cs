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
using Fido2IdentityServer.Utilities;
using Fido2NetLib;
using Fido2NetLib.Objects;
using IdentityModel;
using IdentityModel.Jwk;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
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


           
            var existingDbKeys = _authContext.FidoLogins.Where(x => x.UserId == user.Id).Select(x => x.PublicKeyIdBytes);
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
                
                RequireResidentKey = true,
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
         //   AuthenticationExtensionsClientInputs
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
                    var users = _authContext.FidoLogins.Where( l => l.PublicKeyIdBytes.SequenceEqual(args.CredentialId));
                    if (users.Count() > 0) return false;

                    return true;
                };

                // 2. Verify and make the credentials
                var success = await _lib.MakeNewCredentialAsync(model, options, callback);
                var dbUser = _authContext.Users.First(x => x.Id == user.Id);
                dbUser.TwoFactorEnabled = true;
                var login = new FidoLogin()
                {
                    PublicKeyIdBytes = success.Result.CredentialId,
                    PublicKeyId = Fido2NetLib.Base64Url.Encode(success.Result.CredentialId),
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

        [HttpGet]
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

        [HttpGet]
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

        [HttpGet]
        public async Task<IActionResult> PaymentDigitalSignature(string paymentId, string returnUrl)
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

            if (string.IsNullOrEmpty(paymentId))
            {
                if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Devices");
            }
            var vm = BuildPaymentDigitalSignatureViewModel(paymentId);

            //TODO Defend from null pointers
            AddAuthenticatorsToViewModel(vm, user);
            HttpContext.Session.SetString("fido2.returnUrl", string.IsNullOrEmpty(returnUrl) ? string.Empty : returnUrl);
           // paymentChallenge.Challenge = challenge;
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AssertDigitalSignature([FromForm] string authenticator, [FromHeader] string PaymentId)
        {
            var sub = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            var user = await _users.FindByIdAsync(sub);
            
            if (string.IsNullOrEmpty(authenticator))
            {
                //var vm = BuildFido2LoginViewModel(returnUrl, rememberLogin, user);
                //return View("Fido2Login", vm);
                return Json(new { });
            }

            var fidoLogin = _authContext.FidoLogins.First(x => x.AaGuid == authenticator && x.UserId == user.Id);
            var existingKeys = new List<PublicKeyCredentialDescriptor>();
            existingKeys.Add(new PublicKeyCredentialDescriptor(fidoLogin.PublicKeyIdBytes));

            var coseStruct = CBORObject.DecodeFromBytes(fidoLogin.PublicKey);
            var key = JsonConvert.DeserializeObject<Identity.Models.CredentialPublicKey>(coseStruct.ToJSONString());


            //Convert header to string
            var headerString = AlgorithmUtilities.GetJWTHeader(key, coseStruct.ToJSONString());

            //Convert header to bytes
            var headerBytes = System.Text.Encoding.UTF8.GetBytes(headerString);

            //Getting data to sign
            var payment = _authContext.Payments.First(x => x.Id == PaymentId);
            var payloadObj = new
            {
                paymentId = payment.Id,
                sub = payment.UserId,
            };
            var payloadStr = JsonConvert.SerializeObject(payloadObj);
            var payload = Encoding.UTF8.GetBytes(payloadStr);

         // var jwk = new IdentityModel.Jwk.JsonWebKey(headerString);
         //   var challenge = IdentityModel.Base64Url.Encode(headerBytes) + "." + Fido2NetLib.Base64Url.Encode(payload);
            var challenge = Fido2NetLib.Base64Url.Encode(payload);
            HttpContext.Session.SetString("fido2.assertionChallenge", challenge);
            HttpContext.Session.SetString("fido2.paymentId", PaymentId);
            var exts = new AuthenticationExtensionsClientInputs() { SimpleTransactionAuthorization = "FIDO", GenericTransactionAuthorization = new TxAuthGenericArg { ContentType = "text/plain", Content = new byte[] { 0x46, 0x49, 0x44, 0x4F } }, UserVerificationIndex = true, Location = true, UserVerificationMethod = true };
            var uv = UserVerificationRequirement.Preferred;
            var options = _lib.GetAssertionOptions(
                existingKeys,
                uv,
                exts
            );

            options.Challenge = Encoding.UTF8.GetBytes(challenge);
           
            // 4. Temporarily store options, session/in-memory cache/redis/db
            HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());
            return Json(options);
        }

        [HttpPost]
        public async Task<IActionResult> AssertDigitalSignatureResult([FromBody] AuthenticatorAssertionRawResponse clientResponse)
        {
            var sub = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                return Json(new { success = false });
            }
            var user = await _users.FindByIdAsync(sub);
            if (user == null)
            {
                return Json(new { success = false });
            }
            try
            {
                // 1. Get the assertion options we sent the client
                var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
                var options = AssertionOptions.FromJson(jsonOptions);

                // 2. Get registered credential from database
                var creds = _authContext.FidoLogins.FirstOrDefault(x => x.PublicKeyIdBytes.SequenceEqual(clientResponse.Id) && x.UserId == user.Id);
                //DemoStorage.GetCredentialById(clientResponse.Id);

                if (creds == null)
                {
                    return Json(new { success = false });
                }

                // 3. Get credential counter from database
                var storedCounter = creds.SignatureCounter;

                // 4. Create callback to check if userhandle owns the credentialId
                IsUserHandleOwnerOfCredentialIdAsync callback = async (args) =>
                {
                    return _authContext.FidoLogins.FirstOrDefault(x => x.UserHandle.SequenceEqual(args.UserHandle) && x.PublicKeyIdBytes.SequenceEqual(args.CredentialId)) != null;
                };

                // 5. Make the assertion
                var res = await _lib.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter, callback);
                if (!string.IsNullOrEmpty(res.ErrorMessage))
                    return Json(new { success = false, error = res.ErrorMessage });

                var paymentId = HttpContext.Session.GetString("fido2.paymentId");
                var payment = _authContext.Payments.First(x => x.Id == paymentId);
                var signature = Fido2NetLib.Base64Url.Encode(clientResponse.Response.Signature);

                payment.HasSignature = true;
                payment.PublicKeyId = creds.PublicKeyId;
                payment.AuthenticatorData = Fido2NetLib.Base64Url.Encode(clientResponse.Response.AuthenticatorData);
                payment.Signature = signature;
                payment.ClientData = Fido2NetLib.Base64Url.Encode(clientResponse.Response.ClientDataJson);

                //HttpContext.Session.SetString("fido2.assertionSignature", signature);
                //HttpContext.Session.SetString("fido2.publicKeyId", creds.PublicKeyId);
                //HttpContext.Session.SetString("fido2.clientData", Fido2NetLib.Base64Url.Encode(clientResponse.Response.ClientDataJson));
                //HttpContext.Session.SetString("fido2.authenticatorData", Fido2NetLib.Base64Url.Encode(clientResponse.Response.AuthenticatorData));
     
                // 6. Store the updated counter
                creds.SignatureCounter = res.Counter;
                _authContext.SaveChanges();

                // 7. return OK to client
                return Json(new { signature, success = true });
            }
            catch (Exception e)
            {
                return Json(new {  success = false });
            }
        }

        public IActionResult DigitalSigningFailed()
        {
            var returnUrl = HttpContext.Session.GetString("fido2.returnUrl");
            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Devices");
            }

            return Redirect(returnUrl);
        }

        public IActionResult DigitalSigningSuccess()
        {
            var returnUrl = HttpContext.Session.GetString("fido2.returnUrl");
            var paymentId= HttpContext.Session.GetString("fido2.paymentId");
            return Redirect(returnUrl + "?paymentId=" + paymentId);
        }

        [ValidateAntiForgeryToken]
        public IActionResult RemoveLogin(long loginId)
        {
            var login = _authContext.FidoLogins.FirstOrDefault(x => x.Id == loginId);
            if (login!= null)
            {
                var userId = login.UserId;
                if (_authContext.FidoLogins.Where(x => x.UserId == userId).Count() == 1)
                {
                    var user = _authContext.Users.First(x => x.Id == userId);
                    user.TwoFactorEnabled = false;
                }
                _authContext.FidoLogins.Remove(login);
                _authContext.SaveChanges();


            }

            return RedirectToAction("Devices");
        }

        private DevicesViewModel BuildDevicesViewModel(User user)
        {
            var vm = new DevicesViewModel();
            var devices = _authContext.FidoLogins.Where(l => l.UserId == user.Id);
            foreach (var device in devices)
            {
                vm.FidoLogins.Add(new FidoLoginViewModel()
                {
                    Id = device.Id,
                    AaGuid = device.AaGuid,
                    CredentialType = device.AuthenticatorName,
                    RegistrationDate = device.RegistrationDate,
                    CredentialId = device.PublicKeyIdBytes != null && device.PublicKeyIdBytes.Length > 0 ? System.Convert.ToBase64String(device.PublicKeyIdBytes) : string.Empty
                });
            }

            return vm;
        }

        private void AddAuthenticatorsToViewModel(PaymentDigitalSignatureViewModel model, User user)
        {
            var authenticators = _authContext.FidoLogins.Where(x => x.UserId == user.Id);           
            foreach (var authenticator in authenticators)
            {
                model.AuthenticatorTypes.Add(new KeyValuePair<string, string>(authenticator.AuthenticatorName, authenticator.AaGuid));
            }

        //    model.AuthenticatorTypes = model.AuthenticatorTypes.Distinct().ToList();
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

        protected PaymentDigitalSignatureViewModel BuildPaymentDigitalSignatureViewModel(string paymentId)
        {
            var payment = _authContext.Payments.FirstOrDefault(x => x.Id == paymentId);

            if (payment == null)
            {
                return null;
            }
            var vm = new PaymentDigitalSignatureViewModel()
            {
                UserId = payment.UserId,
                PaymentId = payment.Id,
                Amount = payment.Amount,
                CreditorAccount = payment.CreditorAccount,
                DebtorAccount = payment.DebtorAccount,
                CreditorName = payment.CreditorName,
                DebtorName = payment.DebtorName
            };

            return vm;

        }
    }
}