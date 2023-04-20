using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OAuthLineDemo.Data;
using OAuthLineDemo.Models;
using System.Data;
using System.Diagnostics;
using OAuthLibrary;
using Utility = OAuthLibrary.Utility;
using Newtonsoft.Json.Linq;
using OAuthLibrary.LineLoginService;
using static OAuthLibrary.LineLoginModel;
using Microsoft.AspNetCore.Identity;
using OAuthLibrary.LineNotifyService;
using System;
using static OAuthLibrary.LineNotifyModel;

namespace OAuthLineDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDbConnection _connection;
        private readonly LineLoginService _lineLoginService;
        private readonly LineNotifyService _lineNotifyService;

        public IConfiguration Configuration { get; }
        public HomeController(ILogger<HomeController> logger, IDbConnection connection, IConfiguration configuration, LineLoginService lineLoginService, LineNotifyService lineNotifyService)
        {
            _logger = logger;
            _connection = connection;
            _lineLoginService = lineLoginService;
            _lineNotifyService = lineNotifyService;
            Configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            // 如果有登入過，目前會把資料存在 cookie 中
            var accessToken = HttpContext.Request.Cookies["AccessToken"];
            var idToken = HttpContext.Request.Cookies["IdToken"];
            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(idToken))
            {
                // 驗證 LineLogin 的 access token
                LineLoginVerifyAccessTokenResult accessTokenVerifyResult = null;
                try
                {
                    accessTokenVerifyResult = await _lineLoginService.VerifyAccessTokenAsync(accessToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return View();
                }

                // 驗證 LineLogin 的 id token
                LineLoginVerifyIdTokenResult idTokenVerifyResult = null;
                try
                {
                    var clientId = Configuration["LineLoginOAuth2:ClientId"];
                    idTokenVerifyResult = await _lineLoginService.VerifyIdTokenAsync(idToken, clientId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return View();
                }

                var user = Utility.GetUserProfile(accessToken);
                var userInfo = new UserInfo
                {
                    email = idTokenVerifyResult.Email,
                    userId = idTokenVerifyResult.Sub,
                    displayName = user.displayName,
                    id_token = idToken,
                    access_token = accessToken
                };

                //如果資料庫內有user id 就更新
                if (_lineLoginService.UserExists(userInfo.userId))
                {
                    _lineLoginService.UpdateUserToken(userInfo);
                }
                else
                {
                    //沒有userid 就新增資料
                    _lineLoginService.InsertUserToken(userInfo);
                }



                //檢查是否有訂閱(有無LineNotifyAccessToken)

                bool isLineNotifyBinded = _lineNotifyService.IsLineNotifyAccessTokenBinded(userInfo.userId);

                ViewBag.User = user;
                ViewBag.IsLineNotifyBinded = isLineNotifyBinded;
                return View();
                
            }
            return View();
            // return RedirectToAction("Index");
        }

        public IActionResult LineLogin()
        {
            var clientId = Configuration["LineLoginOAuth2:ClientId"];
            var redirectUri = Configuration["LineLoginOAuth2:RedirectUri"];
            var scope = Configuration["LineLoginOAuth2:Scope"];
            //state 要額外處理 避免 CSRF 攻擊
            var url = $"https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id={clientId}&state=123123&scope={scope}&redirect_uri={redirectUri}";
            return Redirect(url);

        }

        public IActionResult LineLoginCallback()
        {
            if (!Request.Query.TryGetValue("code", out var code))
            {
                return StatusCode(400);
            }
            var ClientId = Configuration["LineLoginOAuth2:ClientId"];
            var ClientSecret = Configuration["LineLoginOAuth2:ClientSecret"];
            var CallbackUrl = Configuration["LineLoginOAuth2:RedirectUri"];
            var Token = Utility.GetTokenFromCode(code, ClientId, ClientSecret, CallbackUrl);

         
            //寫入db
            //// 取得 id token 物件後，將相關資訊塞到 cookie 中
            HttpContext.Response.Cookies.Append("AccessToken", Token.access_token);
            HttpContext.Response.Cookies.Append("ExpiresIn", Token.expires_in.ToString());
            HttpContext.Response.Cookies.Append("IdToken", Token.id_token);
            HttpContext.Response.Cookies.Append("RefreshToken", Token.refresh_token);
            HttpContext.Response.Cookies.Append("Scope", Token.scope);
            HttpContext.Response.Cookies.Append("TokenType", Token.token_type);

            return RedirectToAction("Index");
        }


        //訂閱LineNotify
        public async Task<IActionResult> BindLineNotify()
        {
            // 驗證現在的 IdToken 是否有效
            var idToken = HttpContext.Request.Cookies["IdToken"];

            LineLoginVerifyIdTokenResult idTokenVerifyResult = null;
            try
            {
                var clientId = Configuration["LineLoginOAuth2:ClientId"];
                idTokenVerifyResult = await _lineLoginService.VerifyIdTokenAsync(idToken, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return RedirectToAction("Index");
            }


            var lnclientId = Configuration["LineNotifyOAuth2:ClientId"];
            var redirectUri = Configuration["LineNotifyOAuth2:RedirectUri"];
            var state = "123123";//要另外處理,避免CSRF攻擊
            var url = $"https://notify-bot.line.me/oauth/authorize?response_type=code&client_id={lnclientId}&redirect_uri={redirectUri}&scope=notify&state={state}";
            return Redirect(url);
        }


        public async Task<IActionResult> LineNotifyCallback()
        {
            //要拿code 去換 token
            if (!Request.Query.TryGetValue("code", out var code))
            {
                return StatusCode(400);
            }

            // 驗證現在的 IdToken 是否有效
            var idToken = HttpContext.Request.Cookies["IdToken"];

            LineLoginVerifyIdTokenResult idTokenVerifyResult = null;
            try
            {
                var clientId = Configuration["LineLoginOAuth2:ClientId"];
                idTokenVerifyResult = await _lineLoginService.VerifyIdTokenAsync(idToken, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return RedirectToAction("Index");
            }

            var lnclientId = Configuration["LineNotifyOAuth2:ClientId"];
            var redirectUri = Configuration["LineNotifyOAuth2:RedirectUri"];
            var clientSecret = Configuration["LineNotifyOAuth2:ClientSecret"];

            // 透過 code 取得 acccess token
            var lineNotifyAccessToken = await _lineNotifyService.GetAccessTokenAsync(code, lnclientId, clientSecret, redirectUri);
            // 更新資料庫綁定狀態
            _lineNotifyService.UpdateLineNotifyAccessToken(lineNotifyAccessToken, idTokenVerifyResult.Sub);


            //可發送Line內建的圖
            //https://developers.line.biz/en/docs/messaging-api/sticker-list/
            string stickerPackageId = "11537";
            string stickerId = "52002734";
            await _lineNotifyService.SendMessageAsync(lineNotifyAccessToken, "綁定成功!" , stickerPackageId, stickerId);

            return RedirectToAction("Index");
        }



        /// <summary>
        /// 撤銷 Line Notify 的 access token
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> RevokeLineNotify()
        {
            // 驗證現在的 IdToken 是否有效
            var idToken = HttpContext.Request.Cookies["IdToken"];

            LineLoginVerifyIdTokenResult idTokenVerifyResult = null;
            try
            {
                var clientId = Configuration["LineLoginOAuth2:ClientId"];
                idTokenVerifyResult = await _lineLoginService.VerifyIdTokenAsync(idToken, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return RedirectToAction("Index");
            }

            // 先從資料庫中取得 access token
            var lineNotifyAccessToken = await _lineNotifyService.GetLineNotifyAccessTokenAsync(idTokenVerifyResult.Sub);

            //清除 sub 的 access token
            _lineNotifyService.ClearLineNotifyAccessTokenAsync(idTokenVerifyResult.Sub);

            //撤銷 access token
            await _lineNotifyService.RevokeAccessTokenAsync(lineNotifyAccessToken);

            return RedirectToAction("Index");
        }




        public async Task<IActionResult> LineLogout()
        {

            var accessToken = HttpContext.Request.Cookies["AccessToken"];
            var clientId = Configuration["LineLoginOAuth2:ClientId"];
            var clientSecret = Configuration["LineLoginOAuth2:ClientSecret"];

            try
            {
                // 撤銷 Line Login 的 access token
                await _lineLoginService.RevokeAccessTokenAsync(accessToken, clientId, clientSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            // 刪除 cookie 資料
            HttpContext.Response.Cookies.Delete("AccessToken");
            HttpContext.Response.Cookies.Delete("ExpiresIn");
            HttpContext.Response.Cookies.Delete("IdToken");
            HttpContext.Response.Cookies.Delete("RefreshToken");
            HttpContext.Response.Cookies.Delete("Scope");
            HttpContext.Response.Cookies.Delete("TokenType");

            return RedirectToAction("Index");
        }


        public IActionResult Privacy()
        {
           
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}