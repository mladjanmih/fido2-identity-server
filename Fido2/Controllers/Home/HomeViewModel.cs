using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Controllers.Home
{
    public class HomeViewModel
    {
        public HomeRegisterViewModel Register { get; set; } = new HomeRegisterViewModel();

        public HomeLoginViewModel Login { get; set; } = new HomeLoginViewModel();

    }

    public class HomeRegisterViewModel
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public bool HasError { get; set; }
    }

    public class HomeLoginViewModel
    {
        public string Username { get; set; }

        public string Password { get; set; }

    }
}
