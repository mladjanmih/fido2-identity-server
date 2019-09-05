using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fido2IdentityServer.Identity.Models
{
    public class User: IdentityUser
    {
        public string DisplayName { get; set; }
        public virtual ICollection<FidoLogin> FidoLogins { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Identity.IdentityRole{System.String}" />
    public class Role : IdentityRole<string>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Identity.IdentityUserRole{System.String}" />
    public class UserRole : IdentityUserRole<string>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Identity.IdentityUserClaim{System.String}" />
    public class UserClaim : IdentityUserClaim<string>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Identity.IdentityUserLogin{System.String}" />
    public class UserLogin : IdentityUserLogin<string>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Identity.IdentityRoleClaim{System.String}" />
    public class RoleClaim : IdentityRoleClaim<string>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Identity.IdentityUserToken{System.String}" />
    public class UserToken : IdentityUserToken<string>
    {
    }
}
