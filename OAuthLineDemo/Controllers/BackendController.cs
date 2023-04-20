using Dapper;
using Microsoft.AspNetCore.Mvc;
using OAuthLibrary.LineLoginService;
using OAuthLibrary.LineNotifyService;
using System.Data;
using static OAuthLibrary.LineNotifyModel;
using System.Linq;
using OAuthLineDemo.Models;

namespace OAuthLineDemo.Controllers
{
    public class BackendController : Controller
    {
        private readonly IDbConnection _connection;
        private readonly LineLoginService _lineLoginService;
        private readonly LineNotifyService _lineNotifyService;
        public IConfiguration Configuration { get; }
        public BackendController(ILogger<HomeController> logger, IDbConnection connection, IConfiguration configuration, LineLoginService lineLoginService, LineNotifyService lineNotifyService)
        {
           
            _connection = connection;
            _lineLoginService = lineLoginService;
            _lineNotifyService = lineNotifyService;
            Configuration = configuration;
        }



        public IActionResult Index()
        {
            return View();
        }



        public IActionResult GetSendMessageTable(string MessageInput)
        {
            if (!string.IsNullOrEmpty(MessageInput))
            {
                //找出有LineNotifyAccessToken的user 再發訊息
                var AllUser = _lineNotifyService.GetAllUserLineNotifyAccessToken();
                foreach (var user in AllUser)
                {
                    _ = _lineNotifyService.SendMessageAsync(user.AccessToken, MessageInput, "", "");
                }

                //寫入資料庫
                _lineNotifyService.InsertMessage(MessageInput);
            }

            var sql = "SELECT [Message],convert(varchar, CreateDATE, 120) 'CreateDATE'  FROM [WEB_TEST_NET].[dbo].[SendMsgRecord] WITH(NOLOCK) ORDER BY CreateDATE DESC";
            var items = _connection.Query<SendMsgRecordViewModel>(sql).ToList();
            return PartialView("GetSendMessageTable", items);
        }

        public IActionResult DelSendMessageTable()
        {
            var sql = "DELETE FROM [WEB_TEST_NET].[dbo].[SendMsgRecord] ";
            var items = _connection.Query<SendMsgRecordViewModel>(sql);
            return PartialView("GetSendMessageTable", items);

        }
    }
}
