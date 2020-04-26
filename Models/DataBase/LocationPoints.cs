using System.ComponentModel.DataAnnotations;

namespace TelegramBotApp.Models.DataBase
{
    public class LocationPoints
    {
        [Key]
        public int IdPoint { get; set; }
        public int IdAdmin { get; set; }
        public string NamePoint { get; set; }
        public string DescriptionPoint { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public byte[] ImagePoint { get; set; }
        public bool ExpectNamePoint { get; set; } = true;
        public bool ExpectLocation { get; set; } = false;
        public bool ExpectImagePoint { get; set; } = false;
    }
}
