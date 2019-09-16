using Fido2IdentityServer.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Utilities
{
    public class CertificateUtilities
    {
        public static X509Certificate2 GetAndValidateCertificate(string base64Cert)
        {
            try
            {
                //Get certificate that contains public key that was used for payload signing
                var certificate = new X509Certificate2(Convert.FromBase64String(base64Cert));
                var verifyResult = certificate.Verify();
                if (!verifyResult)
                {
                    return null;
                }

                return certificate;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static X509Certificate2 GetAndValidateCertificate(string base64Cert, AuthenticationContext context)
        {
            try
            {
                //Get certificate that contains public key that was used for payload signing
                var certificate = new X509Certificate2(Convert.FromBase64String(base64Cert));
                var userCert = context.UserCertificates.FirstOrDefault(x => x.Thumbprint == certificate.Thumbprint);
                if (userCert == null)
                {
                    return null;
                }

                var verifyResult = certificate.Verify();
                if (!verifyResult)
                {
                    return null;
                }

                var userCertificate = new X509Certificate2(Convert.FromBase64String(userCert.Certificate));
                return userCertificate;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
