using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCardSigner.Services
{
    public abstract class CertificateProvider
    {
        public static CertificateProvider GetProvider()
        {
            return new FileCertificateProvider();
        }

        public abstract bool LoadCertificate();

        public abstract Chilkat.Cert GetCertificate();
    }
}
