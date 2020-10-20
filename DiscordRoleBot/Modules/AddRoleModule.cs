using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRoleBot.Modules
{
    public class AddRoleModule : ModuleBase<SocketCommandContext>
    {
        [Command("addrole")]
        [Summary("adds a specified role (if it exists) to a specified user (if they exist)")]
        [Helpers.Authorise("staff")]
        public async Task AddRoleAsync([Remainder] [Summary("The role to add to a comma separated list of users")] string parameters = null)
        {
            string userLookup = Context.User.ToString();
            SocketGuildUser requester = Bot.GetSocketGuildUser(userLookup);
            string reply = "Something went wrong, not sure what.";
            if (Context.IsPrivate)
            {
                if (parameters != null)
                {
                    // we need to be careful here as we can't just split by space as some of the roles and username descriminator combos have spaces
                    int openQuotePos = parameters.IndexOf('"');
                    if (openQuotePos != -1)
                    {
                        int closeQuotePos = parameters.IndexOf('"', openQuotePos + 1);
                        int length = closeQuotePos - (openQuotePos + 1);
                        string roleString = parameters.Substring(openQuotePos + 1, length);
                        SocketRole role = Bot.GetRole(roleString);
                        if (role != null)
                        {
                            parameters = parameters.Substring(closeQuotePos + 1);
                            string[] parametersTokens = parameters.Split(',');
                            int totalNumber = parametersTokens.Length;
                            int count = 0;
                            foreach(string parameterToken in parametersTokens)
                            {
                                string partialReply = "Something went wrong and I don't know what.\n";
                                string roleAddee = parameterToken.Trim();
                                if (roleAddee.Contains('#'))
                                {
                                    // discord user lookup
                                    // should return university id and user details
                                    SocketGuildUser discordUser = Bot.GetSocketGuildUser(roleAddee);
                                    if (discordUser != null)
                                    {
                                        //matches server discord user
                                        ulong discordSnowflake = discordUser.Id;
                                        Student student = null;
                                        if (StudentsFile.Instance.TryGetDiscordStudent(discordSnowflake, out student))
                                        {
                                            _ = Bot.AddRoleToUser(discordUser, role);
                                            partialReply = "I have added the role: " + roleString + " to user: " + discordUser.Username + "#" + discordUser.Discriminator + " (" + student.StudentId + ")\n";
                                            
                                        }
                                        else
                                        {
                                            partialReply = "The Discord username and discriminator (" + discordUser.Username + "#" + discordUser.Discriminator + ") does not match a student in our records. Please check that you have typed it correctly. They may not have verified their Discord user using the Canvas quiz.\n";
                                        }
                                    }
                                    else
                                    {
                                        partialReply = "The Discord username and discriminator (" + roleAddee + ") does not match a Discord user on the server. Please check that you have typed it correctly\n";
                                    }
                                }
                                else if (roleAddee.Length == 9)
                                {
                                    int studentId = 0;
                                    if (int.TryParse(roleAddee, out studentId))
                                    {
                                        Student student = null;
                                        if (StudentsFile.Instance.TryGetStudent(studentId, out student))
                                        {
                                            // this discord user matches one of the students
                                            SocketGuildUser discordUser = Bot.GetSocketGuildUser(student.DiscordSnowflake);
                                            if (discordUser != null)
                                            {
                                                _ = Bot.AddRoleToUser(discordUser, role);
                                                partialReply = "I have added the role: " + roleString + " to user: " + discordUser.Username + "#" + discordUser.Discriminator + " (" + student.StudentId + ")\n";
                                            }
                                            else
                                            {
                                                partialReply = "The student id provided (" + roleAddee + ") is in the list, but may have left the server.\n";
                                            }
                                        }
                                        else
                                        {
                                            partialReply = "The student id provided (" + roleAddee + ") does not match our records. Please check that you have typed it correctly. They may not have joined the Discord server.\n";
                                        }
                                    }
                                }
                                reply = count is 0 ?
                                    partialReply : reply += partialReply;
                                count++;
                            }
                        }
                        else
                        {
                            reply = "The role " + roleString + " does not exist on the server. Please check that you have typed it correctly.";
                        }
                    }
                    else
                    {
                        // no space so there is a shortage of parameters
                        reply = "You attempted to use this command without its required parameters: the role you want to add, and the student id that you want to add it to.";
                    }
                }
                else
                {
                    // no parameter provided to command
                    reply = "You attempted to use this command without its required parameters: the role you want to add, and the student id that you want to add it to.";
                }
            }
            else
            {
                reply = "Please send me this request in a Direct Message (you can reply to this message if you like). You should delete your previous public message if you can.";
            }

            Bot.SendMessage(requester, reply);
            string requesterLookup = requester.Username + "#" + requester.Discriminator + " (" + requester.Nickname + ")";
            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "AddRoleModule", "[AddRole]: " + requesterLookup + " was told: " + reply));
        }

    }
}
