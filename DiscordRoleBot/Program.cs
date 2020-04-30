using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    class Program
    {
        private static DiscordSocketClient _client = null;
        private static IConfiguration _config = null;

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
            var notifyId = (UInt64)(_config.GetValue(Type.GetType("System.UInt64"), "Notify"));
            SocketUser notifyUser = _client.GetUser(notifyId);
            if (notifyUser != null)
            {
                _ = notifyUser.GetOrCreateDMChannelAsync().ContinueWith(SendMessage, "I'm back baby!");
            }
            else
            {
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", "notifyUser is null"));
            }           

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
            if (task.Status == TaskStatus.RanToCompletion)
            {
                task.Result.SendMessageAsync(arg2.ToString()).ContinueWith(SentMessage, task.Result);
            }
            else
            {
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", "Tried to send message " + arg2.ToString() + " but it failed."));
                var notifyId = (UInt64)(_config.GetValue(Type.GetType("System.UInt64"), "Notify"));
                _client.GetUser(notifyId).GetOrCreateDMChannelAsync().ContinueWith(SendMessage, "Tried to send message " + arg2.ToString() + " but it failed.");
            }
        }

        private static void SentMessage(Task<IUserMessage> task, object arg2)
        {
            IDMChannel channel = arg2 as IDMChannel;
            if (!task.IsCompletedSuccessfully)
            {                
                _ = Log(new LogMessage(LogSeverity.Error, "Bot", "Tried to send message " + channel.Name + " but it failed."));
                var notifyId = (UInt64)(_config.GetValue(Type.GetType("System.UInt64"), "Notify"));
                _client.GetUser(notifyId).GetOrCreateDMChannelAsync().ContinueWith(SendMessage, "Tried to send message to " + channel.Name + " but it failed.");                
            }
            else
            {
                _ = Log(new LogMessage(LogSeverity.Info, "Bot", "Sent message " + task.Result.Content + " to " + channel.Name + ".")); 
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
    }
}
