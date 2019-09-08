using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Utilities
{
    public static class StringExtensions
    {
        public static string UrlDecodeBase64(this string s)
        {
            string incoming = s
             .Replace('_', '/').Replace('-', '+');
            switch (s.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            return incoming;
        }


    }
}
