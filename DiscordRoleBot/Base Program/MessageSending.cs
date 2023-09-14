using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    internal partial class Bot
    {
        private static Dictionary<Guid, (int Retries, object Content)> _messages = new Dictionary<Guid, (int, object)>();

        /// <summary>
        /// Queues and sends a message to the specified user. If sending fails a number of retries will be attempted.
        /// Failures (retries all unsuccessful) will be logged and additionally sent to those on the 'notify' list (where possible).
        /// </summary>
        /// <param name="user">User to send message to</param>
        /// <param name="message">Message to send to user</param>
        internal static void SendMessage(SocketUser user, string message)
        {
            Guid messageId = Bot.AddMessageToQueue(user, message);
            _ = user.CreateDMChannelAsync().ContinueWith(Bot.SendMessage, messageId);
        }

        private static Guid AddMessageToQueue(SocketUser user, string message)
        {
            Guid notificationId = Guid.NewGuid();
            _messages.Add(notificationId, (0, (user, message)));
            return notificationId;
        }
        private static void SendMessage(Task<IDMChannel> task, object messageId)
        {
            _messages.TryGetValue((Guid)messageId, out var m);
            var message = ((SocketUser User, string Notification))m.Content;

            if (task.Status == TaskStatus.RanToCompletion && task.Result is IDMChannel)
            {
                m.Retries = 0;
                m.Content = (task.Result, message.Notification);
                task.Result.SendMessageAsync(message.Notification.ToString()).ContinueWith(SentMessage, messageId);
            }
            else
            {
                string errorMessage = "Tried to send message " + message.Notification.ToString() + " to " + message.User.Username + " but it failed when trying to get a DM channel.";
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage + " Retrying..."));
                m.Retries++;
                if (m.Retries <= 2)
                {
                    message.User.CreateDMChannelAsync().ContinueWith(SendMessage, messageId);
                }
                else
                {
                    if (!_config.GetSection("NotifyList").Get<ulong[]>().Contains(message.User.Id))
                    {
                        Notify(errorMessage);
                        _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage));
                    }
                    _messages.Remove((Guid)messageId);
                }
            }
        }

        private static void SentMessage(Task<IUserMessage> task, object arg2)
        {
            _messages.TryGetValue((Guid)arg2, out var m);
            var message = ((SocketUser User, string Notification))m.Content;

            if (task.Status == TaskStatus.RanToCompletion)
            {
                _messages.Remove((Guid)arg2);
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Bot", "Sent message " + task.Result.Content + " to " + message.User.Username + "#" + message.User.Discriminator + "."));
            }
            else
            {
                string errorMessage = "Tried to send message " + message.Notification + " to " + message.User.Username + "#" + message.User.Discriminator + " but it failed.";
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage + " Retrying..."));
                m.Retries++;
                if (m.Retries <= 2)
                {
                    message.User.SendMessageAsync(message.Notification.ToString()).ContinueWith(SentMessage, arg2);
                }
                else
                {
                    if (!_config.GetSection("NotifyList").Get<ulong[]>().Contains(message.User.Id))
                    {
                        Notify(errorMessage);
                        _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage));
                    }
                    _messages.Remove((Guid)arg2);
                }
            }
        }

        /// <summary>
        /// Sends a given message to all the users specified in the appsettings.json file
        /// NotifyList section
        /// </summary>
        /// <param name="notification">the string message to be sent to the notify users</param>
        internal static void Notify(string notification)
        {
            var arrayOfNotifyIds = _config.GetSection("NotifyList").Get<ulong[]>();
            foreach (ulong notifyId in arrayOfNotifyIds)
            {
                Notify(notifyId, notification);
            }
        }

        private static void Notify(ulong userId, string notification)
        {
            SocketUser user = _client.GetUser(userId);
            user = _client.GetUser("davidparkerdr");
            if (user != null)
            {
                Notify(user, notification);
            }
            else
            {
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", "user with id: " + userId + " is not found"));
            }
        }

        private static void Notify(SocketUser user, string notification)
        {
            SendMessage(user, notification);
        }

        
    }
}
