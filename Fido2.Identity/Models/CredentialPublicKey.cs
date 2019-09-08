using Newtonsoft.Json;

namespace Fido2IdentityServer.Identity.Models
{
    public class CredentialPublicKey
    {
        [JsonProperty("1")]
        public string KeyType { get; set; }

        [JsonProperty("3")]
        public string Algorithm { get; set; }

    }

    public class RSACredentialPublicKey : CredentialPublicKey
    {
        [JsonProperty("-1")]
        public string N { get; set; }

        [JsonProperty("-2")]
        public string E { get; set; }
    }

    public class ECCredentialPublicKey : CredentialPublicKey
    {
        [JsonProperty("-1")]
        public string Curve { get; set; }

        [JsonProperty("-2")]
        public string X { get; set; }

        [JsonProperty("-3")]
        public string Y { get; set; }
    }
}