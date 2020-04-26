using System;
using System.Configuration;
using Telegram.Bot.Types;
using System.Drawing;
using System.Linq;
using Telegram.Bot;
using TelegramBotApp.Models.DataBase;
using System.IO;
using MySql.Data.MySqlClient;
using System.Net;

namespace TelegramBotApp.Models.Commands
{
    public class MakePoint
    {
        public static void InsertPoint(Message message, MainDbContext context, TelegramBotClient client)
        {
            var newPoint = new LocationPoints()
            {
                IdAdmin = message.From.Id
            };
            context.AllPoints.Add(newPoint);
            context.SaveChanges();

            client.SendTextMessageAsync(message.From.Id, "Enter name of the point:");
        }

        public static void CheckOnAddInformationToPoint(MainDbContext context, Message message, TelegramBotClient client)
        {
            var userId = message.From.Id;

            try
            {
                int lastPoint = LastPoint(context, message);

                if (IsExpectNamePoint(context, lastPoint, userId))
                {
                    if (NamePointNotNull(context, lastPoint, userId))
                    {
                        if (DescriptionPointNotNull(context, lastPoint, userId))
                        {
                            if (LocationNotNull(context, lastPoint, userId))
                            {
                                if (ImagePointNotNull(context, lastPoint, userId))
                                {
                                    client.SendTextMessageAsync(message.From.Id, "Record created successfully!");
                                }
                                else
                                {
                                    if (!IsExpectImagePoint(message))
                                    {
                                        ExpectImagePointTrue(message);
                                        client.SendTextMessageAsync(message.From.Id, "Send Image:");
                                    }
                                }
                            }
                            else
                            {
                                if (!IsExpectLocation(message))
                                {
                                    ExpectLocationTrue(context, lastPoint, userId);
                                    client.SendTextMessageAsync(message.From.Id, "Send location:");
                                }
                            }
                        }
                        else
                        {
                            UpdateDescriptionPoint(context, message, lastPoint, userId);
                            ExpectLocationTrue(context, lastPoint, userId);
                            client.SendTextMessageAsync(message.From.Id, "Send location:");
                        }
                    }
                    else
                    {
                        UpdateNamePoint(context, message, lastPoint, userId);
                        client.SendTextMessageAsync(message.From.Id, "Enter description of the point:");
                    }
                }
                else
                {
                    client.SendTextMessageAsync(message.From.Id, "Wrong command", replyToMessageId: message.MessageId);
                }
            }
            catch (Exception e)
            {
                client.SendTextMessageAsync(userId, $"CheckOnAddInformationToPointError: {e.Message}");
            }
        }

        public static void OperationDelete(Message message, MainDbContext context, TelegramBotClient client)
        {
            if (int.TryParse(message.Text, out int n))
            {
                var index = Convert.ToInt32(message.Text);
                if (LastPoint(context, message) >= index && index > 0)
                {
                    DeleteSelectPoint(index, context);
                    client.SendTextMessageAsync(message.From.Id, "Point delete!");
                    if (CountAllPoint(context) == 0)
                    {
                        AdminRightsCommand.OperationClear(message);
                        client.SendTextMessageAsync(message.From.Id, "No records to delete.");
                    }
                }
                else
                {
                    client.SendTextMessageAsync(message.From.Id, "Wrong command", replyToMessageId: message.MessageId);
                    OutpuAllPoint(message, context, client);
                    client.SendTextMessageAsync(message.From.Id, "Enter the delete field index:");
                }
            }
            else
            {
                client.SendTextMessageAsync(message.From.Id, "Wrong command", replyToMessageId: message.MessageId);
                OutpuAllPoint(message, context, client);
                client.SendTextMessageAsync(message.From.Id, "Enter the delete field index:");
            }
        }

        public static void DeleteSelectPoint(int index, MainDbContext context)
        {
            var parameter = new MySqlParameter("@index", index);
            context.Database.ExecuteSqlCommand("DELETE FROM locationpoints " +
                "WHERE IdPoint = @index", parameter);
        }

        public static int CountAllPoint(MainDbContext context)
        {
            var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints");
            var count = 0;
            foreach (var point in points)
            {
                if (point.NamePoint != null)
                {
                    count++;
                }
            }
            return count;
        }
        public static void OutpuAllPoint(Message message, MainDbContext context, TelegramBotClient client)
        {
            var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints");

            foreach (var point in points)
            {
                if (point.NamePoint != null)
                {
                    client.SendTextMessageAsync(message.From.Id, $"{point.IdPoint} - {point.NamePoint}");
                }
            }
        }

        public static byte[] ImageToByte(Image image, TelegramBotClient client, Message message)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, image.RawFormat);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception e)
            {
                client.SendTextMessageAsync(message.From.Id, $"Error2: {e.Message}");
                return new byte[1];
            }
        }

        public static bool IsExpectLocation(Message message)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    context.Database.CreateIfNotExists();
                    connection.Open();

                    var lastPoint = LastPoint(context, message);
                    var userId = message.From.Id;
                    var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
                    var parameter2 = new MySqlParameter("@userId", userId);
                    var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints " +
                        "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);

                    foreach (var point in points)
                    {
                        if (point.ExpectLocation)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        public static void UpdateLocation(Message message)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    var userId = message.From.Id;
                    int lastPoint = LastPoint(context, message);
                    var parameter1 = new MySqlParameter("@longitude", Convert.ToDouble(message.Location.Longitude));
                    var parameter2 = new MySqlParameter("@latitude", Convert.ToDouble(message.Location.Latitude));
                    var parameter3 = new MySqlParameter("@lastPoint", lastPoint);
                    var parameter4 = new MySqlParameter("@userId", userId);
                    context.Database.ExecuteSqlCommand("UPDATE locationpoints SET Longitude = @longitude, Latitude = @latitude " +
                        "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2, parameter3, parameter4);
                }
            }
        }

        public static void UpdateImagePoint(Message message, TelegramBotClient client, Telegram.Bot.Types.File file)
        {
            var link = "https://api.telegram.org/file/bot" + $"{ConfigurationManager.AppSettings["KeyBot"]}/{file.FilePath}";
            var image = Image.FromStream(new MemoryStream(new WebClient().DownloadData(link)));
            var userImage = ImageToByte(image, client, message);
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    connection.Open();
                    var lastPoint = context.AllPoints.Max(p => p.IdPoint);
                    var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
                    var parameter2 = new MySqlParameter("@userImage", userImage)
                    {
                        MySqlDbType = MySqlDbType.Blob
                    };
                    context.Database.ExecuteSqlCommand("UPDATE locationpoints SET ImagePoint = @userImage WHERE IdPoint = @lastPoint", parameter1, parameter2);
                }
            }
        }

        private static int LastPoint(MainDbContext context, Message message)
        {
            var parameter1 = new MySqlParameter("@userId", message.From.Id);
            var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints " +
                "WHERE IdAdmin = @userId", parameter1);
            var last = 0;

            foreach (var point in points)
            {
                last = point.IdPoint;
            }
            return last;
        }

        private static bool IsExpectNamePoint(MainDbContext context, int lastPoint, int userId)
        {
            var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
            var parameter2 = new MySqlParameter("@userId", userId);
            var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints " +
                "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);

            foreach (var point in points)
            {
                if (point.ExpectNamePoint)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsExpectImagePoint(Message message)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    var lastPoint = LastPoint(context, message);
                    var userId = message.From.Id;
                    var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
                    var parameter2 = new MySqlParameter("@userId", userId);
                    var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints " +
                        "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);

                    foreach (var point in points)
                    {
                        if (point.ExpectImagePoint)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        private static bool NamePointNotNull(MainDbContext context, int lastPoint, int userId)
        {
            var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
            var parameter2 = new MySqlParameter("@userId", userId);
            var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints " +
                "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);

            foreach (var point in points)
            {
                if (point.NamePoint != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool DescriptionPointNotNull(MainDbContext context, int lastPoint, int userId)
        {
            var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
            var parameter2 = new MySqlParameter("@userId", userId);
            var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints " +
                "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);

            foreach (var point in points)
            {
                if (point.DescriptionPoint != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool LocationNotNull(MainDbContext context, int lastPoint, int userId)
        {
            var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
            var parameter2 = new MySqlParameter("@userId", userId);
            var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints " +
                "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);

            foreach (var point in points)
            {
                if (point.Latitude != null && point.Longitude != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ImagePointNotNull(MainDbContext context, int lastPoint, int userId)
        {
            var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
            var parameter2 = new MySqlParameter("@userId", userId);
            var points = context.Database.SqlQuery<LocationPoints>("SELECT * FROM locationpoints " +
                "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);

            foreach (var point in points)
            {
                if (point.ImagePoint != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static void UpdateNamePoint(MainDbContext context, Message message, int lastPoint, int userId)
        {
            var parameter1 = new MySqlParameter("@messageText", message.Text);
            var parameter2 = new MySqlParameter("@lastPoint", lastPoint);
            var parameter3 = new MySqlParameter("@userId", userId);
            context.Database.ExecuteSqlCommand("UPDATE locationpoints SET NamePoint = @messageText " +
                "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2, parameter3);
        }

        private static void UpdateDescriptionPoint(MainDbContext context, Message message, int lastPoint, int userId)
        {
            var parameter1 = new MySqlParameter("@messageText", message.Text);
            var parameter2 = new MySqlParameter("@lastPoint", lastPoint);
            var parameter3 = new MySqlParameter("@userId", userId);
            context.Database.ExecuteSqlCommand("UPDATE locationpoints SET DescriptionPoint = @messageText " +
                "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2, parameter3);
        }

        private static void ExpectLocationTrue(MainDbContext context, int lastPoint, int userId)
        {
            var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
            var parameter2 = new MySqlParameter("@userId", userId);
            context.Database.ExecuteSqlCommand("UPDATE locationpoints SET ExpectLocation = 1 " +
                "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);
        }

        public static void ExpectImagePointTrue(Message message)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    var lastPoint = LastPoint(context, message);
                    var userId = message.From.Id;

                    var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
                    var parameter2 = new MySqlParameter("@userId", userId);
                    context.Database.ExecuteSqlCommand("UPDATE locationpoints SET ExpectImagePoint = 1 " +
                        "WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);
                }
            }
        }

        public static void ClearExpectDataOfPoint(Message message)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    var lastPoint = LastPoint(context, message);
                    var userId = message.From.Id;

                    var parameter1 = new MySqlParameter("@lastPoint", lastPoint);
                    var parameter2 = new MySqlParameter("@userId", userId);
                    context.Database.ExecuteSqlCommand("UPDATE locationpoints SET ExpectNamePoint = 0, ExpectLocation = 0," +
                        "ExpectImagePoint = 0 WHERE IdPoint = @lastPoint AND IdAdmin = @userId", parameter1, parameter2);
                }
            }
        }
    }
}
