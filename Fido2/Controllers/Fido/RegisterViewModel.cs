using Fido2NetLib;
namespace Fido2IdentityServer.Controllers.Fido
{
    public class RegisterViewModel
    {

        public string Challenge { get; set; }
        public CredentialCreateOptions CredentialCreateOptions { get; set; }
    }
}
