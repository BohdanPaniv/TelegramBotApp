using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotApp.Models.DataBase;

namespace TelegramBotApp.Models.Commands
{
    public class AdminRightsCommand : Command
    {
        public override string Name => "/adminrights";

        public override void Execute(Message message, TelegramBotClient client)
        {
            try
            {
                using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
                {
                    using (var context = new MainDbContext(connection, false))
                    {
                        context.Database.CreateIfNotExists();
                        connection.Open();

                        if (IsAdmin(message, context))
                        {
                            client.SendTextMessageAsync(message.From.Id, "Select operation:\n/InsertPoint or /DeletePoint");
                            UpdateExpectOperation(message, context);
                        }
                        else
                        {
                            if (!IsRecord(message, context))
                            {
                                InsertRecord(message, context);
                            }

                            if (!IsExpectLogin(message, context))
                            {
                                UpdateExpectLogin(message, context);
                            }

                            client.SendTextMessageAsync(message.From.Id, "Enter your login:");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                client.SendTextMessageAsync(message.From.Id, $"AdminRightsExecuteError: {e.Message}.");
            }
        }

        public static void CheckOnAdminRequest(Message message, TelegramBotClient client)
        {
            try
            {
                using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
                {
                    using (var context = new MainDbContext(connection, false))
                    {
                        context.Database.CreateIfNotExists();
                        connection.Open();

                        if (!IsAdmin(message, context))
                        {
                            if (IsExpectLogin(message, context))
                            {
                                if (IsUserLoginNotNull(message, context))
                                {
                                    if (!IsUserPasswordNotNull(message, context))
                                    {
                                        UpdateUserPassword(message, context);
                                    }

                                    if (IsCheckOnAdminComplete(message, context))
                                    {
                                        AdminTrue(message, context);
                                        ClearUserInput(message, context);
                                        client.SendTextMessageAsync(message.From.Id, "Congratulation! You have administrator rights.\n" +
                                            "Select operation:\n/InsertPoint or /DeletePoint");
                                        UpdateExpectOperation(message, context);
                                    }
                                    else
                                    {
                                        ClearUserInput(message, context);
                                        client.SendTextMessageAsync(message.From.Id, "Incorrect login or password. Try again /adminrights");
                                    }
                                }
                                else
                                {
                                    UpdateUserLogin(message, context);
                                    client.SendTextMessageAsync(message.From.Id, "Enter your password:");
                                }
                            }
                            else
                            {
                                client.SendTextMessageAsync(message.From.Id, "Wrong command", replyToMessageId: message.MessageId);
                            }
                        }
                        else
                        {
                            CheckOperation(message, context, client);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                client.SendTextMessageAsync(message.From.Id, $"CheckOnAdminRequestError: {e.Message}");
            }
        }

        public static void CheckOperation(Message message, MainDbContext context, TelegramBotClient client)
        {
            if (IsExpectOperation(message, context))
            {
                if (IsOperationNotNull(message, context))
                {
                    if (IsAddPoint(message, context))
                    {
                        MakePoint.CheckOnAddInformationToPoint(context, message, client);
                    }
                    else
                    {
                        MakePoint.OperationDelete(message, context, client);
                    }
                }
                else
                {
                    if (IsExpectOperation(message, context))
                    {
                        if (message.Text == "/InsertPoint")
                        {
                            UpdateOperation(message, context, 1);
                            MakePoint.InsertPoint(message, context, client);
                        }
                        else if (message.Text == "/DeletePoint")
                        {
                            UpdateOperation(message, context, 2);

                            if (MakePoint.CountAllPoint(context) != 0)
                            {
                                MakePoint.OutpuAllPoint(message, context, client);
                                client.SendTextMessageAsync(message.From.Id, "Enter the delete field index:");
                            }
                            else
                            {
                                OperationClear(message);
                                client.SendTextMessageAsync(message.From.Id, "No records to delete.");
                            }
                        }
                        else
                        {
                            client.SendTextMessageAsync(message.From.Id, "Wrong command", replyToMessageId: message.MessageId);
                        }
                    }
                    else
                    {
                        client.SendTextMessageAsync(message.From.Id, "Wrong command", replyToMessageId: message.MessageId);
                    }
                }
            }
            else
            {
                client.SendTextMessageAsync(message.From.Id, "Wrong command.", replyToMessageId: message.MessageId);
            }
        }

        public static bool IsAddPoint(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>($"SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.Operation == 1)
                {
                    return true;
                }
            }
            return false;
        }

        public static void OperationClear(Message message)
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                using (var context = new MainDbContext(connection, false))
                {
                    var parameter = new MySqlParameter("@userId", message.From.Id);
                    context.Database.ExecuteSqlCommand($"UPDATE adminrights SET Operation = 0, ExpectOperation = 0" +
                        $" WHERE IdUser = @userId", parameter);
                }
            }
        }

        public static void UpdateExpectOperation(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand("UPDATE adminrights SET ExpectOperation = 1 WHERE " +
                "IdUser = @userId", parameter);
        }

        public static bool IsExpectOperation(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>($"SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.ExpectOperation)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsExpectDeletePoint(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>($"SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.ExpectDeletePoint)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsOperationNotNull(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>("SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.Operation != 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static void UpdateOperation(Message message, MainDbContext context, int index)
        {
            var parameter1 = new MySqlParameter("@index", index);
            var parameter2 = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand("UPDATE adminrights SET Operation = @index WHERE " +
                "IdUser = @userId", parameter1, parameter2);
        }

        private void InsertRecord(Message message, MainDbContext context)
        {
            var newUser = new AdminRights()
            {
                IdUser = message.From.Id
            };
            context.AllAdmins.Add(newUser);
            context.SaveChanges();
        }

        private void UpdateExpectLogin(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand("UPDATE adminrights SET ExpectLogin = 1 WHERE " +
                "IdUser = @userId", parameter);
        }
        public static bool IsExpectLogin(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>($"SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.ExpectLogin)
                {
                    return true;
                }
            }
            return false;
        }
        private bool IsRecord(Message message, MainDbContext context)
        {
            var userId = message.From.Id;
            var users = context.Database.SqlQuery<AdminRights>("SELECT * FROM adminrights");

            foreach (var user in users)
            {
                if (user.IdUser == userId)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsAdmin(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>($"SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.IsAdmin)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsUserLoginNotNull(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>("SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.UserLogin != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsUserPasswordNotNull(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>("SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.UserPassword != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsCheckOnAdminComplete(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            var users = context.Database.SqlQuery<AdminRights>("SELECT * FROM adminrights WHERE IdUser = @userId", parameter);

            foreach (var user in users)
            {
                if (user.UserLogin == ConfigurationManager.AppSettings["AdminLogin"] && user.UserPassword == ConfigurationManager.AppSettings["AdminPassword"])
                {
                    return true;
                }
            }
            return false;
        }

        private static void UpdateUserPassword(Message message, MainDbContext context)
        {
            var parameter1 = new MySqlParameter("@messageText", message.Text);
            var parameter2 = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand("UPDATE adminrights SET UserPassword = @messageText WHERE " +
                "IdUser = @userId", parameter1, parameter2);
        }

        private static void UpdateUserLogin(Message message, MainDbContext context)
        {
            var parameter1 = new MySqlParameter("@messageText", message.Text);
            var parameter2 = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand("UPDATE adminrights SET UserLogin = @messageText " +
                "WHERE IdUser = @userId", parameter1, parameter2);
        }

        private static void ClearUserInput(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand($"UPDATE adminrights SET UserLogin = null, UserPassword = null, ExpectLogin = 0 WHERE IdUser = @userId", parameter);
        }

        private static void AdminTrue(Message message, MainDbContext context)
        {
            var parameter = new MySqlParameter("@userId", message.From.Id);
            context.Database.ExecuteSqlCommand($"UPDATE adminrights SET IsAdmin = 1 WHERE IdUser = @userId", parameter);
        }
    }
}
