using System.ComponentModel.DataAnnotations;

namespace TelegramBotApp.Models.DataBase
{
    public class DataPoint
    {
        [Key]
        public int IdPoint { get; set; }
        //public int IdUser { get; set; }
        public string NamePoint { get; set; }
        public string DescriptionPoint { get; set; }
        public byte ImagePoint { get; set; }
        public int PositiveFeedback { get; set; } = 0;
        public int NegativeFeedback { get; set; } = 0;
        public bool WaitNamePoint { get; set; } = false;
        public bool WaitDescriptionPoint { get; set; } = false;
        public bool WaitImagePoint { get; set; } = false;
    }
}