using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotApp.Models;
using TelegramBotApp.Models.Commands;

namespace TelegramBotApp.Controllers
{
    public class MessageController : ApiController
    {
        // Webhook.
        [Route(@"api/message/update")]
        public async Task<OkResult> Update([FromBody]Update update)
        {
            IReadOnlyList<Command> commands = Bot.Commands;
            Message message = update.Message;

            TelegramBotClient client = await Bot.Get();

            switch (message.Type)
            {
                case MessageType.Text:
                    try
                    {
                        if (Regex.IsMatch(message.Text, @"\/[a-z]+$"))
                        {
                            var IsExecute = false;

                            foreach (var command in commands)
                            {
                                if (command.Contains(message.Text))
                                {
                                    command.Execute(message, client);
                                    IsExecute = true;
                                    break;
                                }
                            }
                            if (!IsExecute)
                            {
                                await client.SendTextMessageAsync(message.From.Id, "Wrong command", replyToMessageId: message.MessageId);
                            }
                        }
                        else
                        {
                            try
                            {
                                if (OutputLocationCommand.IsExpectDistance(message))
                                {
                                    OutputLocationCommand.Output(message, client);
                                }
                                else
                                {
                                    AdminRightsCommand.CheckOnAdminRequest(message, client);
                                }
                            }
                            catch (Exception e)
                            {
                                await client.SendTextMessageAsync(message.From.Id, $"Error: {e.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        await client.SendTextMessageAsync(message.From.Id, $"Message: {e.Message}");
                    }
                    break;
                case MessageType.Location:
                    try
                    {
                        if (MakePoint.IsExpectLocation(message))
                        {
                            MakePoint.UpdateLocation(message);
                            MakePoint.ExpectImagePointTrue(message);
                            await client.SendTextMessageAsync(message.From.Id, "Send photo in document type:");
                        }
                        else
                        {
                            if (OutputLocationCommand.IsExpectDistance(message))
                            {
                                OutputLocationCommand.AvailablePoints(message, client);
                            }
                            else
                            {
                                WhereAmICommand.FindLocation(message, client);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        await client.SendTextMessageAsync(message.From.Id, $"ErrorLocation: {e.Message}");
                    }
                    break;
                case MessageType.Document:
                    try
                    {
                        if (MakePoint.IsExpectImagePoint(message))
                        {
                            File file = await client.GetFileAsync(fileId: message.Document.FileId);
                            MakePoint.UpdateImagePoint(message, client, file);
                            MakePoint.ClearExpectDataOfPoint(message);
                            AdminRightsCommand.OperationClear(message);
                            await client.SendTextMessageAsync(message.From.Id, "Record created successfully!");
                        }
                        else
                        {
                            await client.SendTextMessageAsync(message.From.Id, "Wrong command", replyToMessageId: message.MessageId);
                        }
                    }
                    catch (Exception e)
                    {
                        await client.SendTextMessageAsync(message.From.Id, $"ErrorPhoto: {e.Message}");
                    }
                    break;
                default:
                    break;
            }

            return Ok();
        }
    }
}
