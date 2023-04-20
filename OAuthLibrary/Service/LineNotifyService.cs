using Dapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static OAuthLibrary.LineLoginModel;
using static OAuthLibrary.LineNotifyModel;

namespace OAuthLibrary.LineNotifyService
{
    public class LineNotifyService
    {
        private readonly IDbConnection _connection;
        private readonly HttpClient _httpClient;
        public LineNotifyService(IDbConnection connection, IHttpClientFactory httpClientFactory)
        {
            _connection = connection;
            _httpClient = httpClientFactory.CreateClient("LineNotifyService");
        }


        //IsLineNotifyAccessTokenBinded
        public bool IsLineNotifyAccessTokenBinded(string userId)
        {
            var sql = @"SELECT TOP 1 LineNotifyAccessToken
                FROM [WEB_TEST_NET].[dbo].[UserToken] WITH(NOLOCK)
                WHERE UserId = @userId AND LineNotifyAccessToken IS NOT NULL";
            var dynamicParams = new DynamicParameters();
            dynamicParams.Add("@userId", userId);
            string lineNotifyAccessToken = _connection.QueryFirstOrDefault<string>(sql, dynamicParams);
            return !string.IsNullOrEmpty(lineNotifyAccessToken); 
        }


        /// <summary>
        /// 依照 code 取得 access token
        /// </summary>
        /// <param name="code"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="returnUri"></param>
        /// <returns></returns>
        public async Task<string> GetAccessTokenAsync(string code, string clientId, string clientSecret, string returnUri)
        {
            var endpoint = "https://notify-bot.line.me/oauth/token";
            var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", returnUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            }));
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            return JsonSerializer.Deserialize<LineNotifyAccessToken>(responseStream).AccessToken;
        }


        //更新綁定Notify
        public void UpdateLineNotifyAccessToken(string acctoken, string userid)
        {
            string sql = $@"UPDATE [dbo].[UserToken] SET [LineNotifyAccessToken] = @accessToken WHERE UserId = @userId";
            var dynamicParams = new DynamicParameters();

            dynamicParams.Add("@accessToken", acctoken);
            dynamicParams.Add("@userId", userid);
            _connection.Query(sql, dynamicParams);
        }

        /// <summary>
        /// 取消訂閱 清除LineNotifyAccessToken
        /// </summary>
        /// <param name="userid"></param>
        public void ClearLineNotifyAccessTokenAsync(string userid)
        {
            string sql = $@"UPDATE [dbo].[UserToken] SET [LineNotifyAccessToken] = @accessToken WHERE UserId = @userId";
            var dynamicParams = new DynamicParameters();

            dynamicParams.Add("@accessToken", null);
            dynamicParams.Add("@userId", userid);
            _connection.Query(sql, dynamicParams);
        }

        /// <summary>
        /// 撤銷 Line Notify 的 access token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task RevokeAccessTokenAsync(string accessToken)
        {
            var endpoint = "https://notify-api.line.me/api/revoke";
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }


        public async Task<string> GetLineNotifyAccessTokenAsync(string sub)
        {
            var sql = @"SELECT TOP 1 LineNotifyAccessToken
                        FROM [WEB_TEST_NET].[dbo].[UserToken] WITH(NOLOCK)
                        WHERE UserId = @userId";
            var dynamicParams = new DynamicParameters();
            dynamicParams.Add("@userId", sub);
            string lineNotifyAccessToken = _connection.QueryFirstOrDefault<string>(sql, dynamicParams);
            return lineNotifyAccessToken;
        }


        /// <summary>
        /// 發送 Line Notify 訊息
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> SendMessageAsync(string accessToken, string message,string stickerPackageId ,string stickerId)
        {
            //可發送Line內建的圖
            //https://developers.line.biz/en/docs/messaging-api/sticker-list/
            //string stickerPackageId = "446";
            //string stickerId = "1989";

            var endpoint = "https://notify-api.line.me/api/notify";
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "message", message },
                { "stickerPackageId", stickerPackageId },
                { "stickerId", stickerId }
            });

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            return true;
        }


        public List<LineNotifyAccessToken> GetAllUserLineNotifyAccessToken()
        {
            var sql = @"SELECT LineNotifyAccessToken 'AccessToken'
                FROM [WEB_TEST_NET].[dbo].[UserToken] WITH(NOLOCK)
                WHERE LineNotifyAccessToken is not null AND LineNotifyAccessToken != ''";

            return _connection.Query<LineNotifyAccessToken>(sql).ToList();
           
        }


        /// <summary>
        /// 紀錄發送訊息
        /// </summary>
        /// <param name="Msg"></param>
        public void InsertMessage(string Msg)
        {
            string sql = $@"INSERT INTO [dbo].[SendMsgRecord]([Message],[CreateDATE]) VALUES (@msg,GETDATE())";
            var dynamicParams = new DynamicParameters();
            dynamicParams.Add("@msg", Msg);

            _connection.Query(sql, dynamicParams);
        }
    }
}
