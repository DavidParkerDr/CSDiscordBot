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
    public class RemoveRoleModule : ModuleBase<SocketCommandContext>
    {
        [Command("removerole")]
        [Summary("removes a specified role (if it exists) from a specified user (if they exist)")]
        public async Task RemoveRoleAsync([Remainder] [Summary("The role to remove and the comma separated list of users to remove it from")] string parameters = null)
        {
            string userLookup = Context.User.ToString();
            SocketGuildUser requester = Program.GetSocketGuildUser(userLookup);
            SocketRole staffRole = Program.GetRole("staff");
            string reply = "Something went wrong, not sure what.";
            if (!requester.Roles.Contains(staffRole))
            {
                // for the time being you need to be staff to ask the bot to add or remove roles
                reply = "You do not have the necessary privileges to perform that action.";
            }
            if (Context.IsPrivate)
            {
                if (parameters != null)
                {
                    // we need to be careful here as we can't just split by space as some of the username descriminator combos have spaces
                    int spacePos = parameters.IndexOf(' ');
                    if (spacePos != -1)
                    {
                        string roleString = parameters.Substring(0, spacePos);
                        SocketRole role = Program.GetRole(roleString);
                        if (role != null)
                        {
                            parameters = parameters.Substring(spacePos);
                            string[] parametersTokens = parameters.Split(',');
                            int totalNumber = parametersTokens.Length;
                            int count = 0;
                            foreach (string parameterToken in parametersTokens)
                            {
                                string partialReply = "Something went wrong and I don't know what.";
                                string roleAddee = parameterToken.Trim();
                                if (roleAddee.Contains('#'))
                                {
                                    // discord user lookup
                                    // should return university id and user details
                                    SocketGuildUser discordUser = Program.GetSocketGuildUser(roleAddee);
                                    if (discordUser != null)
                                    {
                                        //matches server discord user
                                        ulong discordSnowflake = discordUser.Id;
                                        Student student = null;
                                        if (StudentsFile.Instance.TryGetDiscordStudent(discordSnowflake, out student))
                                        {
                                            _ = Program.RemoveRole(discordUser, role);
                                            partialReply = "I have removed the role: " + roleString + " from user: " + discordUser.Username + "#" + discordUser.Discriminator + " (" + student.StudentId + ")";

                                        }
                                        else
                                        {
                                            partialReply = "The Discord username and discriminator (" + discordUser.Username + "#" + discordUser.Discriminator + ") does not match a student in our records. Please check that you have typed it correctly. They may not have verified their Discord user using the Canvas quiz.";
                                        }
                                    }
                                    else
                                    {
                                        partialReply = "The Discord username and discriminator (" + roleAddee + ") does not match a Discord user on the server. Please check that you have typed it correctly";
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
                                            SocketGuildUser discordUser = Program.GetSocketGuildUser(student.DiscordSnowflake);
                                            _ = Program.RemoveRole(discordUser, role);
                                            partialReply = "I have removed the role: " + roleString + " from user: " + discordUser.Username + "#" + discordUser.Discriminator + " (" + student.StudentId + ")";
                                        }
                                        else
                                        {
                                            partialReply = "The student id provided (" + roleAddee + ") does not match our records. Please check that you have typed it correctly. They may not have joined the Discord server.";
                                        }
                                    }
                                }
                                if (count == 0)
                                {
                                    reply = partialReply;
                                }
                                else
                                {
                                    reply += partialReply;
                                }
                                count++;
                            }
                        }
                        else
                        {
                            reply = "That role does not exist on the server. Please check that you have typed it correctly";
                        }
                    }
                    else
                    {
                        // no space so there is a shortage of parameters
                        reply = "You attempted to use this command without its required parameters: the role you want to remove, and the student id that you want to remove it from.";
                    }
                }
                else
                {
                    // no parameter provided to command
                    reply = "You attempted to use this command without its required parameters: the role you want to remove, and the student id that you want to remove it from.";
                }
            }
            else
            {
                reply = "Please send me this request in a Direct Message (you can reply to this message if you like). You should delete your previous public message if you can.";
            }

            Guid replyId = Program.AddMessageToQueue(user, reply);
            _ = user.GetOrCreateDMChannelAsync().ContinueWith(Program.SendMessage, replyId);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<IDMChannel> PackageBackchannel(IDMChannel channel)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return channel;
        }
    }
}
