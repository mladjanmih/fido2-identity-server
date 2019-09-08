// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Fido2IdentityServer.Identity;
using Fido2IdentityServer.Identity.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Account
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private const string _fido2AuthenticationScheme = "fido2.two.factor";
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly UserManager<User> _users;
        private readonly IUserStore<User> _userStore;
        private readonly IResourceOwnerPasswordValidator _validator;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly SignInManager<User> _signInManager;
        private readonly AuthenticationContext _authenticationContext;
        private Fido2 _lib;
        private readonly string _origin;
        private IMetadataService _mds;
        private readonly IJsonHelper _jsonHelper;
        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            UserManager<User> users,
            IUserStore<User> userStore,
            IResourceOwnerPasswordValidator validator,
            IPasswordHasher<User> passwordHasher,
            SignInManager<User> signInManager,
            AuthenticationContext authenticationContext,
            IConfiguration configuration,
            IJsonHelper jsonHelper)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            // this is where you would plug in your own custom identity management library (e.g. ASP.NET Identity)
            _users = users;
            _userStore = userStore;
            _signInManager = signInManager;
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _validator = validator;
            _passwordHasher = passwordHasher;
            _authenticationContext = authenticationContext;
            _jsonHelper = jsonHelper;

            var invalidToken = "6d6b44d78b09fed0c5559e34c71db291d0d322d4d4de0000";
            _origin = configuration["Fido2:Origin"];
            var MDSAccessKey = configuration["fido2:MDSAccessKey"];
            var MDSCacheDirPath = configuration["fido2:MDSCacheDirPath"] ?? Path.Combine(Path.GetTempPath(), "fido2mdscache");
            _mds = string.IsNullOrEmpty(MDSAccessKey) ? null : MDSMetadata.Instance(MDSAccessKey, MDSCacheDirPath);
            if (null != _mds)
            {
                if (false == _mds.IsInitialized())
                    _mds.Initialize().Wait();
            }

            _lib = new Fido2(new Fido2Configuration()
            {
                ServerDomain = configuration["Fido2:ServerDomain"],
                ServerName = "Fido2 Identity Server",
                Origin = _origin,
                MetadataService = _mds
            });
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var info = await HttpContext.AuthenticateAsync(_fido2AuthenticationScheme);
            if (info.Succeeded)
            {
                await HttpContext.SignOutAsync(_fido2AuthenticationScheme);
            }

            var result = await HttpContext.AuthenticateAsync();
            if (result.Succeeded)
            {
                return View("Index");
            }

            if (string.IsNullOrEmpty(returnUrl)) returnUrl = string.Empty;
            // build a model so we know what to show on the login page
            var vm = await BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return RedirectToAction("Challenge", "External", new { provider = vm.ExternalLoginScheme, returnUrl });
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model, string button)
        {
            var info = await HttpContext.AuthenticateAsync(_fido2AuthenticationScheme);
            if (info.Succeeded)
            {
                await HttpContext.SignOutAsync(_fido2AuthenticationScheme);
            }

            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            // the user clicked the "cancel" button
            if (button != "login")
            {
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await _interaction.GrantConsentAsync(context, ConsentResponse.Denied);

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    if (await _clientStore.IsPkceClientAsync(context.ClientId))
                    {
                        // if the client is PKCE then we assume it's native, so this change in how to
                        // return the response is for better UX for the end user.
                        return View("Redirect", new RedirectViewModel { RedirectUrl = model.ReturnUrl });
                    }

                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return Redirect("~/");
                }
            }

            if (ModelState.IsValid)
            {
                // validate username/password against in-memory store
                // var validationContext = new ResourceOwnerPasswordValidationContext() { UserName = model.Username, Password = model.Password };
                // await _validator.ValidateAsync(validationContext);
                var user = await _users.FindByNameAsync(model.Username);
                if (user == null)
                {
                    var viewModel = await BuildLoginViewModelAsync(model.ReturnUrl);
                    return View(viewModel);
                }

                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
                if (result == PasswordVerificationResult.Success)
                {
                    if (user.TwoFactorEnabled)
                    {
                        var claimsIdentity = new ClaimsIdentity();
                        claimsIdentity.AddClaim(new Claim(JwtClaimTypes.Subject, user.Id));

                        await HttpContext.SignInAsync(_fido2AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                        return RedirectToAction("Fido2Login", new { returnUrl = model.ReturnUrl, rememberLogin = model.RememberLogin });
                    }
                    else
                    {


                        await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.ClientId));

                        // only set explicit expiration here if user chooses "remember me". 
                        // otherwise we rely upon expiration configured in cookie middleware.
                        AuthenticationProperties props = null;
                        if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                        {
                            props = new AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                            };
                        };

                        // issue authentication cookie with subject ID and username
                        await HttpContext.SignInAsync(user.Id, user.UserName, props);

                        if (context != null)
                        {
                            if (await _clientStore.IsPkceClientAsync(context.ClientId))
                            {
                                // if the client is PKCE then we assume it's native, so this change in how to
                                // return the response is for better UX for the end user.
                                return View("Redirect", new RedirectViewModel { RedirectUrl = model.ReturnUrl });
                            }

                            // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                            return Redirect(model.ReturnUrl);
                        }

                        // request for a local page
                        if (Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        else if (string.IsNullOrEmpty(model.ReturnUrl))
                        {
                            return Redirect("~/");
                        }
                        else
                        {
                            // user might have clicked on a malicious link - should be logged
                            throw new Exception("invalid return URL");
                        }
                    }
                }

            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(model);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Fido2Login(string returnUrl, bool rememberLogin)
        {
            var info = await HttpContext.AuthenticateAsync(_fido2AuthenticationScheme);
            var tempUser = info?.Principal;
            if (tempUser == null) return RedirectToAction("Login", new { returnUrl });
            var user = await _users.FindByIdAsync(tempUser.GetSubjectId());

            var vm = BuildFido2LoginViewModel(returnUrl, rememberLogin, user);

            HttpContext.Session.SetString("fido2.assertionOptions.returnUrl", string.IsNullOrEmpty(returnUrl) ? string.Empty : returnUrl);
            HttpContext.Session.SetString("fido2.assertionOptions.rememberLogin", rememberLogin.ToString());
            return View(vm);
        }

        [HttpPost]
     //   [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fido2Login([FromForm] string loginType)
        {
            var returnUrl = HttpContext.Session.GetString("fido2.assertionOptions.returnUrl");
            var remLogStr = HttpContext.Session.GetString("fido2.assertionOptions.rememberLogin");
            var rememberLogin = string.IsNullOrEmpty(remLogStr) ? false : bool.Parse(remLogStr);

            var info = await HttpContext.AuthenticateAsync(_fido2AuthenticationScheme);
            var tempUser = info?.Principal;
            if (tempUser == null) return null;//return RedirectToAction("Login", new { returnUrl });
            var user = await _users.FindByIdAsync(tempUser.GetSubjectId());

            if (string.IsNullOrEmpty(loginType))
            {
                //var vm = BuildFido2LoginViewModel(returnUrl, rememberLogin, user);
                //return View("Fido2Login", vm);
                return null;
            }

            var fidoLogins = _authenticationContext.FidoLogins.Where(x => x.UserId == user.Id && x.AuthenticatorName == loginType).Select(x => x.PublicKeyIdBytes);
            var existingKeys = new List<PublicKeyCredentialDescriptor>();
            foreach (var key in fidoLogins)
            {
                existingKeys.Add(new PublicKeyCredentialDescriptor(key));
            }
            var exts = new AuthenticationExtensionsClientInputs() { SimpleTransactionAuthorization = "FIDO", GenericTransactionAuthorization = new TxAuthGenericArg { ContentType = "text/plain", Content = new byte[] { 0x46, 0x49, 0x44, 0x4F } }, UserVerificationIndex = true, Location = true, UserVerificationMethod = true };

            var uv = UserVerificationRequirement.Preferred;
            var options = _lib.GetAssertionOptions(
                existingKeys,
                uv,
                exts
            );
        
            // 4. Temporarily store options, session/in-memory cache/redis/db
            HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());
            return Json(options);
        }

        [HttpPost]
        public async Task<IActionResult> Fido2LoginCallback ([FromBody] AuthenticatorAssertionRawResponse clientResponse)
        {
            var info = await HttpContext.AuthenticateAsync(_fido2AuthenticationScheme);
            var tempUser = info?.Principal;
            if (tempUser == null) return Json(new { success = false });
            var user = await _users.FindByIdAsync(tempUser.GetSubjectId());

            try
            {
                // 1. Get the assertion options we sent the client
                var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
                var options = AssertionOptions.FromJson(jsonOptions);

                // 2. Get registered credential from database
                var creds = _authenticationContext.FidoLogins.FirstOrDefault(x => x.PublicKeyIdBytes.SequenceEqual(clientResponse.Id));
                    //DemoStorage.GetCredentialById(clientResponse.Id);

                if (creds == null)
                {
                    throw new Exception("Unknown credentials");
                }

                if (creds.UserId != user.Id)
                {
                    throw new Exception("User is not owner of credentials.");
                }

                // 3. Get credential counter from database
                var storedCounter = creds.SignatureCounter;

                // 4. Create callback to check if userhandle owns the credentialId
                IsUserHandleOwnerOfCredentialIdAsync callback = async (args) =>
                {
                    return _authenticationContext.FidoLogins.FirstOrDefault(x => x.UserHandle.SequenceEqual(args.UserHandle) && x.PublicKeyIdBytes.SequenceEqual(args.CredentialId)) != null;
                };

                // 5. Make the assertion
                var res = await _lib.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter, callback);

                
                // 6. Store the updated counter
                creds.SignatureCounter = res.Counter;
                _authenticationContext.SaveChanges();

                // 7. return OK to client
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { success = false });
            }
        }

        public async Task<IActionResult> Fido2LoginSuccess()
        {
            var returnUrl = HttpContext.Session.GetString("fido2.assertionOptions.returnUrl");
            var remLogStr = HttpContext.Session.GetString("fido2.assertionOptions.rememberLogin");
            var rememberLogin = string.IsNullOrEmpty(remLogStr) ? false : bool.Parse(remLogStr);
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            var info = await HttpContext.AuthenticateAsync(_fido2AuthenticationScheme);
            var tempUser = info?.Principal;
            if (tempUser == null) return Json(new { success = false });
            var user = await _users.FindByIdAsync(tempUser.GetSubjectId());

            await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.ClientId));

            // only set explicit expiration here if user chooses "remember me". 
            // otherwise we rely upon expiration configured in cookie middleware.
            AuthenticationProperties props = null;
            if (AccountOptions.AllowRememberLogin && rememberLogin)
            {
                props = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                };
            };

            // issue authentication cookie with subject ID and username
            await HttpContext.SignInAsync(user.Id, user.UserName, props);
            await HttpContext.SignOutAsync(_fido2AuthenticationScheme);
            if (context != null)
            {
                if (await _clientStore.IsPkceClientAsync(context.ClientId))
                {
                    // if the client is PKCE then we assume it's native, so this change in how to
                    // return the response is for better UX for the end user.
                    return View("Redirect", new RedirectViewModel { RedirectUrl = returnUrl });
                }

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                return Redirect(returnUrl);
            }

            // request for a local page
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else if (string.IsNullOrEmpty(returnUrl))
            {
                return Redirect("~/");
            }
            else
            {
                // user might have clicked on a malicious link - should be logged
                throw new Exception("invalid return URL");
            }
        }

        public async Task<IActionResult> Fido2LoginFailed()
        {
            var returnUrl = HttpContext.Session.GetString("fido2.assertionOptions.returnUrl");
            var remLogStr = HttpContext.Session.GetString("fido2.assertionOptions.rememberLogin");
            var rememberLogin = string.IsNullOrEmpty(remLogStr) ? false : bool.Parse(remLogStr);

            return RedirectToAction("Fido2Login", new { returnUrl, rememberLogin });
        }

        private Fido2LoginViewModel BuildFido2LoginViewModel(string returnUrl, bool rememberLogin, User user)
        {
            var authenticators = _authenticationContext.FidoLogins.Where(x => x.UserId == user.Id);
            var vm = new Fido2LoginViewModel();
            foreach (var authenticator in authenticators)
            {
                vm.AuthenticatorTypes.Add(authenticator.AuthenticatorName);
            }

            vm.AuthenticatorTypes = vm.AuthenticatorTypes.Distinct().ToList();
            return vm;
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();
                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var result = await HttpContext.AuthenticateAsync();
            if (result.Succeeded)
            {
                return View("Index");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            RegisterViewModel vm = await BuildRegisterViewModelAsync(model);
            if (string.IsNullOrEmpty(model.Username))
            {
                ModelState.AddModelError(string.Empty, "Username is required!");
                vm.HasError = true;
            }
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Password is required!");
                vm.HasError = true;
            }
            if (string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Email is required!");
                vm.HasError = true;
            }

            if (vm.HasError)
            {
                return View(vm);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Password missmatch!");
                vm = await BuildRegisterViewModelAsync(model);
                vm.HasError = true;
                return View(vm);
            }

            var user = new User()
            {
                Email = model.Email,
                LockoutEnabled = false,
                UserName = model.Username
            };

            var result = await _users.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                    vm = await BuildRegisterViewModelAsync(model);
                    vm.HasError = true;
                    return View(vm);
                }
            }

            return RedirectToAction("Index", "Home");      
        }

        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/

        private async Task<RegisterViewModel> BuildRegisterViewModelAsync(RegisterViewModel model)
        {
            return new RegisterViewModel()
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Username = model.Username
            };
        }
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null ||
                            (x.Name.Equals(AccountOptions.WindowsAuthenticationSchemeName, StringComparison.OrdinalIgnoreCase))
                )
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Username = model.Username;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }
    }
}