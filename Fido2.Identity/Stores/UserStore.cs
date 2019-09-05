using Fido2IdentityServer.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fido2IdentityServer.Identity.Stores
{
    public class UserStore: UserStore<User, IdentityRole, AuthenticationContext>
    {
        public UserStore(AuthenticationContext context, IdentityErrorDescriber describer = null) : base(context, describer)
        {

        }
    }
}
