using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordRoleBot
{
    internal partial class Bot
    {
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
            else
            {
                user = _client.GetUser(usernamePlusDescriminator);
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
        public static SocketGuildUser GetSocketGuildUser(string usernamePlusDescriminator, SocketGuild guild = null)
        {
            if (guild == null)
            {
                guild = GetGuild();
            }
            SocketGuildUser guildUser = null;
            SocketUser socketUser = GetSocketUser(usernamePlusDescriminator);
            if (socketUser != null)
            {
                guildUser = guild.GetUser(socketUser.Id);
            }

            return guildUser;
        }
        /// <summary>
        /// gets the socket user from the username and descriminator, then retrieves the guild user
        /// using the user id
        /// </summary>
        /// <param name="discordSnowflake">discord snowflake user id</param>
        /// <param name="guild">the SocketGuild (server) that we are retrieving the user for</param>
        /// <returns>the validated user or null</returns>
        public static SocketGuildUser GetSocketGuildUser(ulong discordSnowflake, SocketGuild guild = null)
        {
            if (guild == null)
            {
                guild = GetGuild();
            }
            SocketGuildUser guildUser = guild.GetUser(discordSnowflake);

            return guildUser;
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
            if (guild == null)
            {
                guild = GetGuild();
            }
            string[] tokens = csvUsernamePlusDescriminators.Split(',');
            List<string> usernamePlusDescriminators = tokens.ToList();
            List<SocketGuildUser> socketGuildUsers = GetSocketGuildUsers(usernamePlusDescriminators, guild);
            return socketGuildUsers;
        }

        private static async void ValidateAllStudentUsers()
        {
            SocketGuild guild = GetGuild();
            IReadOnlyCollection<SocketGuildUser> users = guild.Users;
            foreach (SocketGuildUser user in users)
            {
                SocketRole studentRole = GetRole("student");
                if (user.Roles.Contains(studentRole))
                {
                    // is a student
                    // check against student list
                    Student student = null;
                    if (!StudentsFile.Instance.TryGetDiscordStudent(user.Id, out student))
                    {
                        // discord user is not in student list
                        Console.WriteLine(user.Username + "#" + user.Discriminator);
                    }
                    else
                    {
                        StudentLookupResult canvasResult = await CanvasClient.Instance.GetCanvasUserFrom9DigitId(student.StudentId);
                        Console.Write(user.Username + "#" + user.Discriminator);
                        Console.Write(", " + user.Id);
                        Console.Write(", " + canvasResult.Name);
                        Console.WriteLine(", " + canvasResult.UniId);
                    }
                }
            }
        }

        private static async void FindAllNoRoleUsers()
        {
            SocketGuild guild = GetGuild();
            IReadOnlyCollection<SocketGuildUser> users = guild.Users;
            foreach (SocketGuildUser user in users)
            {
                SocketRole studentRole = GetRole("student");
                if (user.Roles.Contains(studentRole))
                {
                    continue;
                }
                SocketRole staffRole = GetRole("staff");
                if (user.Roles.Contains(staffRole))
                {
                    continue;
                }
                Console.WriteLine(user.Username + "#" + user.Discriminator);
                SocketRole unassignedRole = GetRole("unassigned");
                _ = AddRoleToUser(user, unassignedRole);
            }
        }
    }
}
