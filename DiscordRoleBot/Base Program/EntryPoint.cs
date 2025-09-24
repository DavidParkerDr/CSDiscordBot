using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    internal partial class Bot
    {
        private static DiscordSocketClient _client = null;

        public static IConfiguration _config = null;

        private static bool commandsRegistered = false;
        private static bool canvasThreadStarted = false;

        public static void Main(string[] args)
        {
            
            new Bot().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            var discordSocketConfig = new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All | GatewayIntents.AllUnprivileged | GatewayIntents.GuildPresences
            };
            _client = new DiscordSocketClient(discordSocketConfig);
                        
            _client.Log += FileLogger.Instance.Log;
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            var token = _config.GetValue(Type.GetType("System.String"), "Token").ToString();
            
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Connected += ClientConnectedEventHandler;
            _client.UserJoined += ClientUserJoinedEventHandler;
            _client.Ready += ClientReadyEventHandler;

            // Block this task until the program is closed.
            await Task.Delay(Timeout.Infinite);
        }
       


        #region To Be Removed?

        //private static async void GetCanvasUserAndNotify(int uni9DigitId)
        //{
        //    //StudentLookupResult studentLookupResult = CanvasClient.Instance.GetCanvasUserFrom6DigitId("350809").Result;
        //    StudentLookupResult studentLookupResult = await CanvasClient.Instance.GetCanvasUserFrom9DigitId(uni9DigitId);
        //    if (studentLookupResult != null)
        //    {
        //        Notify("Is this the droid you are looking for? " + studentLookupResult.Name + " <" + studentLookupResult.Email + "> " + studentLookupResult.UniId + " - " + studentLookupResult.LoginId + ".");
        //    }
        //    else
        //    {
        //        Notify("The user with id " + studentLookupResult.UniId + " couldn't be found on Canvas. They may no longer be a student. Try looking them up in SITS");
        //    }
        //}

        //public static Task Log(LogMessage msg)
        //{
        //    if(msg.Severity == LogSeverity.Error)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Red;
        //    }
        //    else
        //    {
        //        Console.ForegroundColor = ConsoleColor.Gray;
        //    }
        //    Console.WriteLine(msg.ToString());
        //    return Task.CompletedTask;
        //}

        #endregion

    }
}
