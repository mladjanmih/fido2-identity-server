using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chilkat;

namespace SmartCardSigner.Services
{
    public class FileCertificateProvider : CertificateProvider
    {
        private readonly string _certPath = "";
        private Chilkat.Cert _cert = null;

        
        public override Cert GetCertificate()
        {
            return _cert;
        }

        public override bool LoadCertificate()
        {
            Chilkat.Cert cert = new Chilkat.Cert();
            var success = cert.LoadFromFile(_certPath);
            if (true != success)
            {
                return false;
            }

            return true;
        }
    }
}
