using System.ComponentModel.DataAnnotations;

namespace TelegramBotApp.Models.DataBase
{
    public class OutputLocations
    {
        [Key]
        public int IdRecord { get; set; }
        public int IdUser { get; set; }
        public string Distance { get; set; }
        public bool ExpectDistance { get; set; } = true;
    }
}
