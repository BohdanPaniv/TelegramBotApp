using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramBotApp.Models.Commands;

namespace TelegramBotApp.Models
{
    public static class Bot
    {
        private static TelegramBotClient client;
        private static List<Command> commandsList;

        public static IReadOnlyList<Command> Commands => commandsList.AsReadOnly();

        public static async Task<TelegramBotClient> Get()
        {
            if (client != null)
            {
                return client;
            }

            commandsList = new List<Command>();
            List<Command> newList = new List<Command>()
            {
                new AdminRightsCommand(),
                new StartCommand(),
                new KeyboardOffCommand(),
                new WhereAmICommand(),
                new OutputLocationCommand()
            };
            commandsList.AddRange(newList);

            client = new TelegramBotClient(ConfigurationManager.AppSettings["KeyBot"]);
            var hook = string.Format(ConfigurationManager.AppSettings["URL"], "api/message/update");
            await client.SetWebhookAsync(hook);

            return client;
        }
    }
}
