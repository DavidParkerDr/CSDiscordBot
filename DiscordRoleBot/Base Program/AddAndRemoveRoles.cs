using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace DiscordRoleBot
{
    internal partial class Bot
    {
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
        public static async Task RemoveRole(string usernamePlusDescriminator, SocketRole role)
        {
            SocketGuild guild = GetGuild();
            if (guild != null)
            {
                SocketGuildUser socketGuildUser = GetSocketGuildUser(usernamePlusDescriminator, guild);
                if (socketGuildUser != null)
                {
                    Task t = RemoveRole(socketGuildUser, role);
                    await t;
                    if(!t.IsCompletedSuccessfully)
                    {
                        throw t.Exception;
                    }
                }
            }
        }
        public static async Task RemoveRole(SocketGuildUser user, SocketRole role)
        {
            Task t = user.RemoveRoleAsync(role);
            await t;
            if (t.IsCompletedSuccessfully)
            {
                //Notify("Hello. I added the role: " + role.Name + " to " + user.Nickname + " (" + user.Username + "#" + user.Discriminator +").");
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Bot", "[RemoveRole] guild role with name: " + role.Name + " is added to: " + user.Nickname + " (" + user.Username + "#" + user.Discriminator + ")"));
            }
            else
            {
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", "[RemoveRole] guild role with name: " + role.Name + " failed to be removed from: " + user.Nickname + " (" + user.Username + "#" + user.Discriminator + ")"));
                throw t.Exception;
            }
        }
        public static async Task AddRoleToUser(SocketGuildUser user, SocketRole role)
        {
            Task t = user.AddRoleAsync(role);
            await t;
            if (t.IsCompletedSuccessfully)
            {
                //Notify("Hello. I added the role: " + role.Name + " to " + user.Nickname + " (" + user.Username + "#" + user.Discriminator +").");
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Bot", "[AddRole] guild role with name: " + role.Name + " is added to: " + user.Nickname + " (" + user.Username + "#" + user.Discriminator + ")"));
            }
            else
            {
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", "[AddRole] guild role with name: " + role.Name + " failed to be added to: " + user.Nickname + " (" + user.Username + "#" + user.Discriminator + ")"));
                throw t.Exception;
            }
        }
        private static async Task AddRoleToUser(SocketGuildUser user, ulong roleId)
        {
            SocketRole role = null;
            role = GetRole(roleId);
            if (role != null)
            {
                Task t = AddRoleToUser(user, role);
                await t;
                if (!t.IsCompletedSuccessfully)
                {
                    throw t.Exception;
                }
            }
            else
            {
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", "[AddRole] guild role with id: " + roleId + " is not found"));
            }
        }
        private static async Task AddRoleToUser(SocketGuildUser user, string roleName)
        {
            SocketRole role = null;
            role = GetRole(roleName);
            if (role != null)
            {
                Task t = AddRoleToUser(user, role);
                await t;
                if (!t.IsCompletedSuccessfully)
                {
                    throw t.Exception;
                }
            }
            else
            {
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "Bot", "[AddRole] guild role with name: " + roleName + " is not found"));
            }
        }

        /// <summary>
        /// Adds a role to a list of users. Will throw an exeption and complete unsuccessfully if no users can be added.
        /// </summary>
        /// <param name="users">List of users to add role to</param>
        /// <param name="role">Role to add to users</param>
        /// <returns>
        /// Tuple (User,Success) where success is a bool that indicates whether the role was successfully added. 
        /// Note, if role addition fails for all users, the task will complete unsuccessfully with 'No users were added successfully'
        /// </returns>
        private static async Task<List<(SocketGuildUser user, bool success)>> AddRoleToUsers(List<SocketGuildUser> users, SocketRole role)
        {
            List<(SocketGuildUser user, bool success)> successList = new List<(SocketGuildUser, bool)>();
            bool outrightFail = true;
            foreach (SocketGuildUser user in users)
            {
                bool success = false;
                Task t = AddRoleToUser(user, role);
                await t;
                if (t.IsCompletedSuccessfully)
                {
                    outrightFail = false;
                    success = true;
                }
                successList.Add((user, success));
            }
            if(outrightFail)
            {
                throw new Exception("No users were added successfully");
            }

            return successList;
        }
        private static async Task AddRoleToUsers(List<string> usernamePlusDescriminators, SocketRole role)
        {
            List<SocketGuildUser> users = GetSocketGuildUsers(usernamePlusDescriminators, GetGuild());
            Task t = AddRoleToUsers(users, role);
            await t;
            if (!t.IsCompletedSuccessfully)
            {
                throw t.Exception;
            }
        }
    }
}
