using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fido2IdentityServer
{
    public class Program
    {

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
              WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
            //.UseKestrel((context, options) =>
            //{
            //    options.Listen(IPAddress.Loopback, 37100);
            //    options.Listen(IPAddress.Loopback, 37101, opts =>
            //    {
            //        opts.UseHttps();
            //    });
            //    options.Listen(IPAddress.Loopback, 37102, opts =>
            //    {
            //        opts.UseHttps(httpsOptions =>
            //        {
            //            X509Certificate2 certificate = new X509Certificate2(Path.Combine("Certificates", "tls-cert.pfx"), "fido2tls");

            //            httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
            //            httpsOptions.ServerCertificate = certificate;
            //        });
            //    });
            //})
            .UseIISIntegration();
    }
}
