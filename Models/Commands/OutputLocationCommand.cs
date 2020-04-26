using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.IO;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotApp.Models.DataBase;

namespace TelegramBotApp.Models.Commands
{
    public class OutputLocationCommand : Command
    {
        public override string Name => "/outputlocation";

        public override void Execute(Message message, TelegramBotClient client)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    var check = false;
                    var users = context.Database.SqlQuery<OutputLocations>("SELECT * FROM outputlocations");

                    foreach (var user in users)
                    {
                        if (user.IdUser == message.From.Id)
                        {
                            check = true;
                        }
                    }
                    if (!check)
                    {
                        var newRecord = new OutputLocations()
                        {
                            IdUser = message.From.Id
                        };
                        context.AllRecord.Add(newRecord);
                        context.SaveChanges();
                    }
                    else
                    {
                        UpdateExpectDistance(context, message);
                    }
                    client.SendTextMessageAsync(message.From.Id, "Send distance(m):");
                }
            }
        }

        public static void Output(Message message, TelegramBotClient client)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    if (DistanceNotNull(context, message))
                    {
                        client.SendTextMessageAsync(message.From.Id, "Send location:");
                    }
                    else
                    {
                        if (int.TryParse(message.Text, out int n))
                        {
                            UpdateDistance(context, message);
                            client.SendTextMessageAsync(message.From.Id, "Send location:");
                        }
                        else
                        {
                            client.SendTextMessageAsync(message.From.Id, "Incorrect data. Send distance:");
                        }
                    }
                }
            }
        }

        public static void AvailablePoints(Message message, TelegramBotClient client)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    try
                    {
                        var distance = GetDistance(context, message);
                        var latitude = Convert.ToDouble(GetLatitude(message));
                        var longitude = Convert.ToDouble(GetLongitude(message));

                        client.SendTextMessageAsync(message.From.Id, "Points in this radius:");

                        var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints ");

                        using (var memoryStream = new MemoryStream())
                        {
                            var count = 0;
                            foreach (var point in points)
                            {
                                var latitudePoint = Convert.ToDouble(point.Latitude);
                                var longitudePoint = Convert.ToDouble(point.Longitude);
                                if (longitudePoint > longitude - 0.00001372 * distance
                                    && longitudePoint < longitude + 0.00001372 * distance
                                    && latitudePoint > latitude - 0.00000954 * distance
                                    && latitudePoint < latitude + 0.00000954 * distance)
                                {
                                    memoryStream.Position = 0;
                                    client.SendTextMessageAsync(message.From.Id, $"Name of point: {point.NamePoint}\n" +
                                        $"Description: {point.DescriptionPoint}");

                                    Thread.Sleep(500);

                                    var longitude2 = float.Parse(point.Longitude);
                                    var latitude2 = float.Parse(point.Latitude);

                                    client.SendLocationAsync(message.From.Id, latitude2, longitude2);

                                    Thread.Sleep(500);


                                    if (point.ImagePoint != null)
                                    {
                                        var imageStream = new MemoryStream();

                                        imageStream.Write(point.ImagePoint, 0, Convert.ToInt32(point.ImagePoint.Length));
                                        imageStream.Position = 0;
                                        client.SendPhotoAsync(message.From.Id, imageStream);
                                        count++;
                                    }

                                    Thread.Sleep(1000);

                                    client.SendTextMessageAsync(message.From.Id, "---------------------------------------------------");
                                }
                            }

                            if (count == 0)
                            {
                                client.SendTextMessageAsync(message.From.Id, "There are no points in this radius");
                            }
                        }
                        ClearData(context, message);
                    }
                    catch (Exception e)
                    {
                        client.SendTextMessageAsync(message.From.Id, $"AvailablePointsError: {e.Message}");
                    }
                }
            }
        }

        public static double GetLatitude(Message message)
        {
            return Convert.ToDouble(message.Location.Latitude);
        }

        public static double GetLongitude(Message message)
        {
            return Convert.ToDouble(message.Location.Longitude);
        }

        public static bool IsExpectDistance(Message message)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    var parameter = new MySqlParameter("@userId", message.From.Id);
                    var users = context.Database.SqlQuery<OutputLocations>($"SELECT * FROM outputlocations WHERE IdUser = @userId", parameter);

                    foreach (var user in users)
                    {
                        if (user.ExpectDistance)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

        }

        public static void UpdateExpectDistance(MainDbContext context, Message message)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand("UPDATE outputlocations SET ExpectDistance = 1 " +
                "WHERE IdUser = @userId", parameter);
        }

        public static bool DistanceNotNull(MainDbContext context, Message message)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<OutputLocations>($"SELECT * FROM outputlocations WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.Distance != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static void UpdateDistance(MainDbContext context, Message message)
        {
            var parameter1 = new MySqlParameter("@messageText", message.Text);
            var parameter2 = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand("UPDATE outputlocations SET Distance = @messageText " +
                "WHERE IdUser = @userId", parameter1, parameter2);
        }

        private static int GetDistance(MainDbContext context, Message message)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<OutputLocations>($"SELECT * FROM outputlocations WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.Distance != null)
                {
                    return Convert.ToInt32(user.Distance);
                }
            }
            return 0;
        }

        public static void ClearData(MainDbContext context, Message message)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand("UPDATE outputlocations SET ExpectDistance = 0, Distance = null " +
                "WHERE IdUser = @userId", parameter);
        }
    }
}
