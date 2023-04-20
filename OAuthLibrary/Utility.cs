using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static OAuthLibrary.LineLoginModel;

namespace OAuthLibrary
{
    public class Utility
    {

        //
        // 摘要:
        //     傳入access_token, 取得用Profile
        //
        // 參數:
        //   access_token:
        public static Profile GetUserProfile(string access_token)
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.Headers.Clear();
                webClient.Headers.Add("Content-Type", "application/json");
                webClient.Headers.Add("Authorization", "Bearer " + access_token);
                byte[] bytes = webClient.DownloadData("https://api.line.me/v2/profile");
                string @string = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<Profile>(@string);
            }
            catch (WebException ex)
            {
                using StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream());
                string text = streamReader.ReadToEnd();
                throw new Exception("GetUserProfile: " + text, ex);
            }
        }

        //
        // 摘要:
        //     從code取得Notify Token (Issue access token)
        //
        // 參數:
        //   code:
        //
        //   ClientId:
        //
        //   ClientSecret:
        //
        //   redirect_uri:
        public static GetTokenFromCodeResult GetTokenFromCode(string code, string ClientId, string ClientSecret, string redirect_uri)
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.Encoding = Encoding.UTF8;
                webClient.Headers.Clear();
                NameValueCollection nameValueCollection = new NameValueCollection();
                nameValueCollection["grant_type"] = "authorization_code";
                nameValueCollection["code"] = code;
                nameValueCollection["redirect_uri"] = redirect_uri;
                nameValueCollection["client_id"] = ClientId;
                nameValueCollection["client_secret"] = ClientSecret;
                byte[] bytes = webClient.UploadValues("https://api.line.me/oauth2/v2.1/token", nameValueCollection);
                string @string = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<GetTokenFromCodeResult>(@string);
            }
            catch (WebException ex)
            {
                using StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream());
                string text = streamReader.ReadToEnd();
                throw new Exception("GetToeknFromCode: " + text, ex);
            }
        }
    }
}
