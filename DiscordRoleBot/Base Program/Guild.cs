using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;

namespace DiscordRoleBot
{
    internal partial class Bot
    {
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
    }
}
