using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OAuthLibrary
{
    public class LineNotifyModel
    {

        public class LineNotifyAccessToken
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
        }
    }
}
