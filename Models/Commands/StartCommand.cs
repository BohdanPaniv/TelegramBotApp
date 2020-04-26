using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotApp.Models.Commands
{
    public class StartCommand : Command
    {
        public override string Name => "/start";
        public override void Execute(Message message, TelegramBotClient client)
        {
            var text = "Сommands:\n/start - print a list of commands;" +
                "\n/adminrights - request for admin rights;\n" +
                "/keyboardoff - delete keyboard;\n/whereami - your geolocation;\n" +
                "/outputlocation - main command.";

            client.SendTextMessageAsync(message.From.Id, text);
        }
    }
}
