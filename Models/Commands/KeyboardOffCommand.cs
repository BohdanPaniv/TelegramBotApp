using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotApp.Models.Commands
{
    public class KeyboardOffCommand : Command
    {
        public override string Name => "/keyboardoff";

        public override void Execute(Message message, TelegramBotClient client)
        {
            client.SendTextMessageAsync(message.From.Id, "Keyboard delete.", replyMarkup: new ReplyKeyboardRemove());
        }
    }
}
