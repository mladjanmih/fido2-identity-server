using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fido2IdentityServer.Utilities
{
    public static class AlgorithmUtilities
    {
        private readonly static Dictionary<int, string> _algorithms = new Dictionary<int, string>
        {
            {-65535, "RS1" },
            {-259, "RS512" },
            {-258, "RS384"},
            {-257, "RS256" },
            {-7, "ES256"}
            //TODO: Add more algorithms
        };


        private readonly static Dictionary<int, string> _curves = new Dictionary<int, string>
        {
            /// <summary> 
            /// NIST P-256 also known as secp256r1
            /// </summary>
            {1, "P256" },
            /// <summary> 
            /// NIST P-384 also known as secp384r1
            /// </summary>
            {2, "P384" },
            /// <summary> 
            /// NIST P-521 also known as secp521r1
            /// </summary>
            {3, "P521" },
            /// <summary> 
            /// X25519 for use w/ ECDH only
            /// </summary>
            {4, "X25519" },
            /// <summary> 
            /// X448 for use w/ ECDH only
            /// </summary>
            {5, "X448" },
            /// <summary> 
            /// Ed25519 for use w/ EdDSA only
            /// </summary>
            {6, "Ed25519" },
            /// <summary> 
            /// Ed448 for use w/ EdDSA only
            /// </summary>
            {7, "Ed448" },
            /// <summary> 
            /// secp256k1 (pending IANA - requested assignment 8)
            /// </summary>
            {8, "P256K" }
        };

        public static string GetAlgorithmName(int id)
        {
            if (_algorithms.ContainsKey(id))
            {
                return _algorithms[id];
            }
            return null;
        }

        public static string GetElipticCurveName(int id)
        {
            if (_curves.ContainsKey(id))
            {
                return _curves[id];
            }
            return null;
        }

        public static string GetJWTHeader(Identity.Models.CredentialPublicKey key, string jsonKey)
        {
            var sb = new StringBuilder();

            //Get algorithm
            var algorithm = AlgorithmUtilities.GetAlgorithmName(int.Parse(key.Algorithm));

            if (algorithm.StartsWith("RS"))
            {
                var rsaKey = JsonConvert.DeserializeObject<Identity.Models.RSACredentialPublicKey>(jsonKey);
                sb.Append($"{{\"kty\":\"RSA\",");
                sb.Append($"\"n\":\"{rsaKey.N}\",");
                sb.Append($"\"e\":\"{rsaKey.E}\",");
                sb.Append($"\"alg\":\"{algorithm}\"");
                sb.Append("}");
            }
            else
            {
                var ecKey = JsonConvert.DeserializeObject<Identity.Models.ECCredentialPublicKey>(jsonKey);
                string curve = null;
                curve = AlgorithmUtilities.GetElipticCurveName(int.Parse(ecKey.Curve));
                sb.Append($"{{\"kty\":\"EC\",");
                sb.Append($"\"crv\":\"{curve}\",");
                sb.Append($"\"x\":\"{ecKey.X}\",");
                sb.Append($"\"y\":\"{ecKey.Y}\",");
                sb.Append($"\"alg\":\"{algorithm}\"");
                sb.Append("}");
            }

            return sb.ToString();
        }
    }
}
