using System.ComponentModel.DataAnnotations;

namespace OAuthLineDemo.Models
{
    public class SendMsgRecordViewModel
    {
        [Display(Name = "發送的內容")]
        public string? Message { get; set; }

        [Display(Name = "時間")]
        public string? CreateDATE { get; set; }
    }
}
