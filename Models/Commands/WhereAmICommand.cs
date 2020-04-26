using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotApp.Models.Commands
{
    public class WhereAmICommand : Command
    {
        public override string Name => "/whereami";

        public override void Execute(Message message, TelegramBotClient client)
        {
            var keyboardButton = new KeyboardButton() { RequestLocation = true, Text = "Geolocation" };
            var replyKeyboard = new ReplyKeyboardMarkup(keyboardButton);

            client.SendTextMessageAsync(message.From.Id, "Geolocation", replyMarkup: replyKeyboard);

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("Telegram Desktop","https://desktop.telegram.org/")
                }
            });

            client.SendTextMessageAsync(message.From.Id, "If you are using Telegram on computer, please " +
                "use the desktop version of Telegram\n" +
                "The keyboard is displayed. Delete the keyboard - /keyboardoff\n", replyMarkup: inlineKeyboard);
        }

        public static void FindLocation(Message message, TelegramBotClient client)
        {
            // Convert to more accurate data.
            var longitude = Convert.ToDouble(message.Location.Latitude);
            var latitude = Convert.ToDouble(message.Location.Longitude);

            var longitudeStr = longitude.ToString();
            var latitudeStr = latitude.ToString();
            longitudeStr = longitudeStr.Replace(",", ".");
            latitudeStr = latitudeStr.Replace(",", ".");

            var link = "https://www.google.com/maps/place/" + $"{longitudeStr},{latitudeStr}";

            var webClient = new WebClient();
            // Get the file in stream.
            using (Stream stream = webClient.OpenRead(link))
            {
                using (var reader = new StreamReader(stream))
                {
                    try
                    {
                        var htmlLine = reader.ReadLine();

                        var regex = new Regex(@"<meta content=[""][^<]*[""]\sitemprop=[""]description[""]>");
                        MatchCollection matches = regex.Matches(htmlLine);
                        var lineEditor = GetSearchString(matches);

                        regex = new Regex(@"[""].*[""]\s");
                        matches = regex.Matches(lineEditor);
                        lineEditor = GetSearchString(matches);

                        lineEditor = lineEditor.Remove(0, 1);
                        lineEditor = lineEditor.Remove(lineEditor.Length - 2, 2);

                        client.SendTextMessageAsync(message.From.Id, "We have your location.", replyToMessageId: message.MessageId);
                        client.SendTextMessageAsync(message.From.Id, "The data is taken from Google maps.\n" +
                            $"Your location:\n{lineEditor}");
                    }
                    catch (Exception e)
                    {
                        client.SendTextMessageAsync(message.From.Id, $"GetLocationError: {e.Message}");
                    }
                }
            }
        }

        private static string GetSearchString(MatchCollection matches)
        {
            var lineEditor = "";
            foreach (Match match in matches)
            {
                lineEditor = match.Value;
            }
            return lineEditor;
        }
    }
}
