using Fido2IdentityServer.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Utilities
{
    public static class JwtUtils
    {
        public static bool ValidateJWT(X509Certificate2 certificate, string token, string algorithm, string payload)
        {
            try
            {

                //Verify signature
                var verifyResult = certificate.Verify();

                //Decode token using public key from certificate
                string decodedPayload = null;
                switch (algorithm)
                {
                    case "RSA":
                        var rsaKey = certificate.GetRSAPublicKey();
                        decodedPayload = Jose.JWT.Decode(token, rsaKey);
                        break;
                    case "DSA":
                        var dsaKey = certificate.GetDSAPublicKey();
                        decodedPayload = Jose.JWT.Decode(token, dsaKey);
                        break;

                    case "EC":
                        var ecKey = certificate.GetECDsaPublicKey();
                        decodedPayload = Jose.JWT.Decode(token, ecKey);
                        break;
                    default:
                        return false;
                }

                //Verify that decoded payload is the same as sent payload
                return payload == decodedPayload;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
