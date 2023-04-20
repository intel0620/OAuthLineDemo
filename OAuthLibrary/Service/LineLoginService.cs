using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data;
using System.Net.Http;
using static OAuthLibrary.LineLoginModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Data.Common;

namespace OAuthLibrary.LineLoginService
{
    public class LineLoginService
    {

        private readonly IDbConnection _connection;
        private readonly HttpClient _httpClient;
        public LineLoginService(IDbConnection connection, IHttpClientFactory httpClientFactory)
        {
            _connection = connection;
            _httpClient = httpClientFactory.CreateClient("LineLoginService");
        }


        public bool UserExists(string userId)
        {
            var sql = $@"SELECT [Id],[IDToken],[AccessToken],[UserId]
                        FROM [WEB_TEST_NET].[dbo].[UserToken] WITH(NOLOCK)
                        WHERE UserId =@userId";
            var dynamicParams = new DynamicParameters();
            dynamicParams.Add("@userId", userId);
            int count = _connection.Query<int>(sql, dynamicParams).FirstOrDefault();
            return count > 0 ? true : false;
           
        }

        public void UpdateUserToken(UserInfo data)
        {
            string sql = $@"UPDATE [dbo].[UserToken] SET [IDToken] = @iDToken,[AccessToken] = @accessToken,[UserId] = @userId,[Name] = @name,[Email] = @email WHERE UserId = @userId";
            var dynamicParams = new DynamicParameters();
            dynamicParams.Add("@iDToken", data.id_token);
            dynamicParams.Add("@accessToken", data.access_token);
            dynamicParams.Add("@userId", data.userId);
            dynamicParams.Add("@name", data.displayName);
            dynamicParams.Add("@email", data.email);
            _connection.Query(sql, dynamicParams);
        }



        public void InsertUserToken(UserInfo data)
        {
            string sql = $@"INSERT INTO [dbo].[UserToken]([IDToken],[AccessToken],[UserId],[Name],[Email]) VALUES (@iDToken,@accessToken,@userId,@name,@email)";
               
            var dynamicParams = new DynamicParameters();
            dynamicParams.Add("@iDToken", data.id_token);
            dynamicParams.Add("@accessToken", data.access_token);
            dynamicParams.Add("@userId", data.userId);
            dynamicParams.Add("@name", data.displayName);
            dynamicParams.Add("@email", data.email);

            _connection.Query(sql, dynamicParams);
           
        }

        /// <summary>
        /// 驗證 Line Login 的 access token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<LineLoginVerifyAccessTokenResult> VerifyAccessTokenAsync(string accessToken)
        {
            var endpoint = $"https://api.line.me/oauth2/v2.1/verify?access_token={accessToken}";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            return JsonSerializer.Deserialize<LineLoginVerifyAccessTokenResult>(responseStream);
        }

        /// <summary>
        /// 驗證 Line Login 的 id token
        /// </summary>
        /// <param name="idToken"></param>
        /// <param name="cliendId"></param>
        /// <returns></returns>
        public async Task<LineLoginVerifyIdTokenResult> VerifyIdTokenAsync(string idToken, string cliendId)
        {
            var endpoint = "https://api.line.me/oauth2/v2.1/verify";
            var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "id_token", idToken },
                { "client_id", cliendId }
            }));
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            return JsonSerializer.Deserialize<LineLoginVerifyIdTokenResult>(responseStream);
        }


        /// <summary>
        /// 撤銷 Line Login 的 access token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <returns></returns>
        public async Task RevokeAccessTokenAsync(string accessToken, string clientId, string clientSecret)
        {
            var endpoint = "https://api.line.me/oauth2/v2.1/revoke";
            var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "access_token", accessToken },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            }));
            response.EnsureSuccessStatusCode();
        }
    }
}
