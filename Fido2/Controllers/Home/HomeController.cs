// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Fido2IdentityServer.Controllers.Account;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Home
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IHostingEnvironment _environment;
        private readonly ILogger _logger;

        public HomeController(IIdentityServerInteractionService interaction, IHostingEnvironment environment, ILogger<HomeController> logger)
        {
            _interaction = interaction;
            _environment = environment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var sub = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            if (!string.IsNullOrEmpty(sub))
            {
                return RedirectToAction("Devices", "Fido");
            }

            var vm = new HomeViewModel();
            TempData["Page"] = "Index";
            return View(vm);
          
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;

                if (!_environment.IsDevelopment())
                {
                    // only show in development
                    message.ErrorDescription = null;
                }
            }

            return View("Error", vm);
        }
    }
}