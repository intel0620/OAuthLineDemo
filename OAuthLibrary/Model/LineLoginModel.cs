using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OAuthLibrary
{
    public class LineLoginModel
    {

        public class UserInfo
        {
            public string access_token { get; set; }
            public string id_token { get; set; }
            public string userId { get; set; }
            public string displayName { get; set; }
            public string email { get; set; }

        }

        //
        // 摘要: 呼叫GetUserProfile後的回傳結果
        public class Profile
        {
            public string displayName { get; set; }

            public string userId { get; set; }

            public string pictureUrl { get; set; }

            public string statusMessage { get; set; }

        }


        //
        // 摘要:呼叫GetTokenFromCode後的回傳結果
        public class GetTokenFromCodeResult
        {
            public string id_token { get; set; }

            public string access_token { get; set; }

            public string token_type { get; set; }

            public decimal expires_in { get; set; }

            public string refresh_token { get; set; }

            public string scope { get; set; }
        }


        public class LineLoginVerifyAccessTokenResult
        {
            [JsonPropertyName("scope")]
            public string Scope { get; set; }

            [JsonPropertyName("client_id")]
            public string CliendId { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }

        public class LineLoginVerifyIdTokenResult
        {
            [JsonPropertyName("iss")]
            public string Iss { get; set; }

            [JsonPropertyName("sub")]
            public string Sub { get; set; }

            [JsonPropertyName("aud")]
            public string Aud { get; set; }

            [JsonPropertyName("exp")]
            public int exp { get; set; }

            [JsonPropertyName("iat")]
            public int Iat { get; set; }

            [JsonPropertyName("auth_time")]
            public int AuthTime { get; set; }

            [JsonPropertyName("nonce")]
            public string Nonce { get; set; }

            [JsonPropertyName("amr")]
            public IEnumerable<string> amr { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("picture")]
            public string Picture { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }
        }
    }
}
