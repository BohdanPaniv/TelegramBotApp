using System.ComponentModel.DataAnnotations;

namespace TelegramBotApp.Models.DataBase
{
    public class AdminRights
    {
        [Key]
        public int IdRecord { get; set; }
        public int IdUser { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool ExpectLogin { get; set; } = true;
        public string UserLogin { get; set; }
        public string UserPassword { get; set; }
        public int Operation { get; set; } = 0;
        public bool ExpectOperation { get; set; } = false;
        public bool ExpectDeletePoint { get; set; } = false;
    }
}
