﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    class Program
    {
        private static DiscordSocketClient _client = null;

        public static IConfiguration _config = null;
        private static Dictionary<Guid, (int Retries, object Content)> _messages = new Dictionary<Guid, (int, object)>();


        public static void Main(string[] args)
        {
            
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig() {AlwaysDownloadUsers = true});
                        
            _client.Log += Log;
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            var token = _config.GetValue(Type.GetType("System.String"), "Token").ToString();
            
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Connected += ClientConnected;
            _client.Ready += ClientReady;

            // Block this task until the program is closed.
            await Task.Delay(Timeout.Infinite);
        }
        /// <summary>
        /// Until the bot client is ready we can't do anything like getting the users from ids
        /// this handler will send a DM to the Notify id specified in the appconfig.json
        /// </summary>
        /// <returns></returns>
        private Task ClientReady()
        {
            Notify("I'm back baby!");

            string users = "DavidParkerDr#6742,JDixonHull#1878";
            _ = AddRoleToUsers(GetSocketGuildUsers(users), GetRole("testrole"));

            GetCanvasUserAndNotify(201503639);
            return Task.CompletedTask;
        }
        /// <summary>
        /// This handler will get called once the bot client has connected to the server.
        /// </summary>
        /// <returns></returns>
        private async Task<Task> ClientConnected()
        {
            return Task.CompletedTask;
        }

        public static void SendMessage(Task<IDMChannel> task, object arg2)
        {
            _messages.TryGetValue((Guid)arg2, out var m);
            var message = ((SocketUser User, string Notification))m.Content;

            if (task.Status == TaskStatus.RanToCompletion && task.Result is IDMChannel)
            {
                m.Retries = 0;
                m.Content = (task.Result, message.Notification);
                task.Result.SendMessageAsync(message.Notification.ToString()).ContinueWith(SentMessage, arg2);
            }
            else
            {
                string errorMessage = "Tried to send message " + message.Notification.ToString() + " to " + message.User.Username + " but it failed when trying to get a DM channel.";
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage + " Retrying..."));
                m.Retries++;
                if (m.Retries <= 2)
                {
                    message.User.GetOrCreateDMChannelAsync().ContinueWith(SendMessage, arg2);
                }
                else
                {
                    if (!_config.GetSection("NotifyList").Get<ulong[]>().Contains(message.User.Id))
                    {
                        Notify(errorMessage);
                        _ = Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage));
                    }
                    _messages.Remove((Guid)arg2);
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
                _ = Log(new LogMessage(LogSeverity.Info, "Bot", "Sent message " + task.Result.Content + " to " + message.User.Username + "#" + message.User.Discriminator + "."));
            }
            else
            {
                string errorMessage = "Tried to send message " + message.Notification + " to " + message.User.Username + "#" + message.User.Discriminator + " but it failed.";
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage + " Retrying..."));
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
                        _ = Log(new LogMessage(LogSeverity.Error, "Bot", errorMessage));
                    }
                    _messages.Remove((Guid)arg2);
                }
            }
        }      

        private static Task Log(LogMessage msg)
        {
            if(msg.Severity == LogSeverity.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private static void Notify(SocketUser user, string notification)
        {
            Guid notificationId = Guid.NewGuid();
            _messages.Add(notificationId, (0, (user, notification)));
            user.GetOrCreateDMChannelAsync().ContinueWith(SendMessage, notificationId);
        }
        private static void Notify(ulong userId, string notification)
        {
            SocketUser user = _client.GetUser(userId);
            if (user != null)
            {
                Notify(user, notification);
            }
            else
            {
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", "user with id: " + userId + " is not found"));
            }
        }
        /// <summary>
        /// Sends a given message to all the users specified in the appsettings.json file
        /// NotifyList section
        /// </summary>
        /// <param name="notification">the string message to be sent to the notify users</param>
        private static void Notify(string notification)
        {
            var arrayOfNotifyIds = _config.GetSection("NotifyList").Get<ulong[]>();
            foreach(ulong notifyId in arrayOfNotifyIds)
            {
                Notify(notifyId, notification);
            }            
        }
        /// <summary>
        /// returns the SocketUser from the client based on the full combo of
        /// username and descriminator eg DavidParkerDr#6742
        /// splits it across the hash # then calls an overloaded version of the method
        /// </summary>
        /// <param name="usernamePlusDescriminator">username and descriminator eg DavidParkerDr#6742</param>
        /// <returns>the validated user or null</returns>
        public static SocketUser GetSocketUser(string usernamePlusDescriminator)
        {
            SocketUser user = null;
            string[] tokens = usernamePlusDescriminator.Split('#');
            if (tokens.Length == 2)
            {
                user = GetSocketUser(tokens[0], tokens[1]);
            }
            return user;
        }
        /// <summary>
        /// returns the SocketUser from the client based on a pair of
        /// username and descriminator eg DavidParkerDr and 6742
        /// </summary>
        /// <param name="username">e.g. DavidParkerDr</param>
        /// <param name="descriminator">e.g. 6742</param>
        /// <returns>the validated user or null</returns>
        public static SocketUser GetSocketUser(string username, string descriminator)
        {
            SocketUser user = _client.GetUser(username, descriminator);
            return user;
        }
        /// <summary>
        /// gets the socket user from the username and descriminator, then retrieves the guild user
        /// using the user id
        /// </summary>
        /// <param name="usernamePlusDescriminator">username and descriminator eg DavidParkerDr#6742</param>
        /// <param name="guild">the SocketGuild (server) that we are retrieving the user for</param>
        /// <returns>the validated user or null</returns>
        public static SocketGuildUser GetSocketGuildUser(string usernamePlusDescriminator, SocketGuild guild)
        {
            SocketGuildUser guildUser = null;
            SocketUser socketUser = GetSocketUser(usernamePlusDescriminator);
            if (socketUser != null)
            {
                guildUser = guild.GetUser(socketUser.Id);
            }
            else
            {
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", "guild user with id: " + usernamePlusDescriminator + " is not found"));
            }

            return guildUser;
        }
        /// <summary>
        /// finds and returns the guild with the matching id
        /// </summary>
        /// <param name="guildId">ulong id</param>
        /// <returns>the guild that matches the id or null</returns>
        public static SocketGuild GetGuild(ulong guildId)
        {
            SocketGuild guild = _client.GetGuild(guildId);
            return guild;

        }
        /// <summary>
        /// looks up the guild id from the appsettings.json file
        /// uses it to find the guild
        /// </summary>
        /// <returns>the guild that matches the id or null</returns>
        public static SocketGuild GetGuild()
        {
            ulong guildId = (ulong)_config.GetValue(Type.GetType("System.UInt64"), "Guild");
            SocketGuild guild = GetGuild(guildId);
            return guild;
        }
        private static async Task RemoveAllRoles(string usernamePlusDescriminator)
        {
            SocketGuild guild = GetGuild();
            SocketGuildUser socketGuildUser = GetSocketGuildUser(usernamePlusDescriminator, guild);
            if (socketGuildUser != null)
            {
                List<SocketRole> userRoles = socketGuildUser.Roles.ToList();
                if (userRoles != null && userRoles.Count != 0)
                {
                    Task t = socketGuildUser.RemoveRolesAsync(userRoles);
                    await t;
                    if (t.IsCompletedSuccessfully)
                    {
                        return;
                    }
                    else
                    {
                        throw t.Exception;
                    }
                }
            }    
        }
        private static async Task RemoveRole(string usernamePlusDescriminator, SocketRole role)
        {
            SocketGuild guild = GetGuild();
            if (guild != null)
            {
                SocketGuildUser socketGuildUser = GetSocketGuildUser(usernamePlusDescriminator, guild);
                if (socketGuildUser != null)
                {
                    _ = RemoveRole(socketGuildUser, role);
                }
            }
        }
        private static async Task RemoveRole(SocketGuildUser user, SocketRole role)
        {
            Task t = user.RemoveRoleAsync(role);
            await t;
            if (t.IsCompletedSuccessfully)
            {
                return;
            }
            else
            {
                throw t.Exception;
            }
        }
        private static async Task AddRoleToUser(SocketGuildUser user, SocketRole role)
        {
            Task t = user.AddRoleAsync(role);
            await t;
            if (t.IsCompletedSuccessfully)
            {
                Notify("Hello. I added the role: " + role.Name + " to " + user.Nickname + " (" + user.Username + "#" + user.Discriminator +").");
            }
            else
            {
                throw t.Exception;
            }
        }
        private static async Task AddRoleToUser(SocketGuildUser user, ulong roleId)
        {
            SocketRole role = null;
            role = GetRole(roleId);
            if (role != null)
            {
                _ = AddRoleToUser(user, role);
            }
            else
            {
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", "[AddRole] guild role with id: " + roleId + " is not found"));
            }
        }
        private static async Task AddRoleToUser(SocketGuildUser user, string roleName)
        {
            SocketRole role = null;
            role = GetRole(roleName);
            if (role != null)
            {
                _ = AddRoleToUser(user, role);
            }
            else
            {
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", "[AddRole] guild role with name: " + roleName + " is not found"));
            }
        }
        private static async Task AddRoleToUsers(List<SocketGuildUser> users, SocketRole role)
        {
            foreach(SocketGuildUser user in users)
            {
                _ = AddRoleToUser(user, role);
            }
        }
        private static async Task AddRoleToUsers(List<string> usernamePlusDescriminators, SocketRole role)
        {
            List<SocketGuildUser> users = GetSocketGuildUsers(usernamePlusDescriminators, GetGuild());
            _ = AddRoleToUsers(users, role);
        }
        private static List<SocketGuildUser> GetSocketGuildUsers(List<string> usernamePlusDescriminators, SocketGuild guild = null)
        {
            if (guild == null)
            {
                guild = GetGuild();
            }
            List<SocketGuildUser> users = new List<SocketGuildUser>();
            foreach (string usernamePlusDescriminator in usernamePlusDescriminators)
            {
                SocketGuildUser user = GetSocketGuildUser(usernamePlusDescriminator, guild);
                if (user != null)
                {
                    users.Add(user);
                }
            }
            return users;
        }
        private static List<SocketGuildUser> GetSocketGuildUsers(string csvUsernamePlusDescriminators, SocketGuild guild = null)
        {
            if(guild == null)
            {
                guild = GetGuild();
            }
            string[] tokens = csvUsernamePlusDescriminators.Split(',');
            List<string> usernamePlusDescriminators = tokens.ToList();
            List<SocketGuildUser> socketGuildUsers = GetSocketGuildUsers(usernamePlusDescriminators, guild);
            return socketGuildUsers;
        }
        /// <summary>
        /// retrieves a guild role from a guild and the roles id
        /// </summary>
        /// <param name="guild">the guild (server) that the role should be from</param>
        /// <param name="roleId">the ulong role id</param>
        /// <returns></returns>
        private static SocketRole GetRole(ulong roleId, SocketGuild guild = null)
        {
            if (guild == null)
            {
                guild = GetGuild();
            }
            SocketRole role = guild.GetRole(roleId);
            return role;
        }
        private static SocketRole GetRole(string roleName, SocketGuild guild = null)
        {
            if(guild == null)
            {
                guild = GetGuild();
            }
            List<SocketRole> guildRoles = guild.Roles.ToList();
            foreach (SocketRole guildRole in guildRoles)
            {
                if(guildRole.Name == roleName)
                {
                    return guildRole;
                }
            }
            return null;
        }
        private static async void GetCanvasUserAndNotify(int uni9DigitId)
        {
           // StudentLookupResult testLookupResult = await CanvasClient.Instance.GetCanvasUserFrom6DigitId("350809");
            StudentLookupResult studentLookupResult = await CanvasClient.Instance.GetCanvasUserFrom9DigitId(uni9DigitId);
            if (studentLookupResult != null)
            {
                Notify("Is this the droid you are looking for? " + studentLookupResult.Name + " <" + studentLookupResult.Email + "> " + studentLookupResult.UniId + " - " + studentLookupResult.LoginId + ".");
            }
            else
            {
                Notify("The user with id " + studentLookupResult.UniId + " couldn't be found on Canvas. They may no longer be a student. Try looking them up in SITS");
            }
        }

        
        

    }
}
