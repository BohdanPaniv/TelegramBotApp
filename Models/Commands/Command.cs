using Telegram.Bot.Types;
using Telegram.Bot;

namespace TelegramBotApp.Models.Commands
{
    public abstract class Command
    {
        public abstract string Name { get; }

        public abstract void Execute(Message message, TelegramBotClient client);

        public bool Contains(string command)
        {
            return command == Name;
        }
    }
}