using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordRoleBot
{
    internal partial class Bot
    {
        /// <summary>
        /// Retrieves a guild role from a guild and the role's id
        /// </summary>
        /// <param name="guild">the guild (server) that the role should be from</param>
        /// <param name="roleId">the ulong role id</param>
        /// <returns></returns>
        public static SocketRole GetRole(ulong roleId, SocketGuild guild = null)
        {
            if (guild == null)
            {
                guild = GetGuild();
            }
            SocketRole role = guild.GetRole(roleId);
            return role;
        }

        /// <summary>
        /// Retrieves a guild role from a guild and the role's name
        /// </summary>
        /// <param name="guild">the guild (server) that the role should be from</param>
        /// <param name="roleName">the string role.Name OR a string containing a role ID</param>
        /// <returns></returns>
        public static SocketRole GetRole(string roleName, SocketGuild guild = null)
        {
            if (guild == null)
            {
                guild = GetGuild();
            }
            List<SocketRole> guildRoles = guild.Roles.ToList();
            foreach (SocketRole guildRole in guildRoles)
            {
                if (guildRole.Name == roleName)
                {
                    return guildRole;
                }
            }
            if (ulong.TryParse(roleName, out ulong roleID))
            { 
                return GetRole(roleID); 
            }
            return null;
        }
    }
}
