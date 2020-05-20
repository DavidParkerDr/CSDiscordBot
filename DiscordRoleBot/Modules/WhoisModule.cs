using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DiscordRoleBot.Modules
{
    public class WhoisModule : ModuleBase<SocketCommandContext>
    {
        [Command("whois")]
        [Summary("looks up user")]
        public async Task GetUserAsync([Remainder] [Summary("The user id to lookup")] string parameters = null)
        {
            string requesterLookup = Context.User.Username + "#" + Context.User.Discriminator;
            SocketGuildUser requester = Program.GetSocketGuildUser(requesterLookup);
            SocketRole staffRole = Program.GetRole("staff");
            string reply = "Something went wrong, not sure what.";
            string lookupString = parameters == null ? "" : parameters.Trim();
            if (!requester.Roles.Contains(staffRole))
            {
                // you need to be staff to do a whois lookup
                reply = "You do not have the necessary privileges to perform that action.";
            }
            else
            {
                if (Context.IsPrivate)
                {
                    if (parameters != null)
                    {
                        if (lookupString.Contains('#'))
                        {
                            // discord user lookup
                            // should return university id and user details
                            SocketGuildUser user = Program.GetSocketGuildUser(lookupString);
                            if (user != null)
                            {
                                //matches server discord user
                                ulong discordSnowflake = user.Id;
                                Applicant applicant = null;
                                if (ApplicantsFile.Instance.TryGetDiscordApplicant(discordSnowflake, out applicant))
                                {
                                    // this discord user matches one of the applicants
                                    reply = "That user (" + lookupString + ") is an applicant. Their applicant id is: " + applicant.ApplicantId + ".";
                                    reply += "For more information, you will need to make an offline request based on their applicant id.";
                                }
                                else
                                {
                                    // they are not an applicant so check for them being a student.
                                    Student student = null;
                                    if (StudentsFile.Instance.TryGetDiscordStudent(discordSnowflake, out student))
                                    {
                                        // this discord user matches one of the applicants
                                        reply = "That user (" + lookupString + ") is an student. Their student id is: " + student.StudentId + ".";
                                        StudentLookupResult studentLookupResult = await CanvasClient.Instance.GetCanvasUserFrom9DigitId(student.StudentId);
                                        if (studentLookupResult != null)
                                        {
                                            if (student.StudentId != studentLookupResult.UniId)
                                            {
                                                //something went wrong in the lookup
                                                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Error, "CanvasLookup", "student.StudentId (" + student.StudentId + ") != studentLookupResult.UniId(" + studentLookupResult.UniId + ")"));
                                            }
                                            else
                                            {
                                                reply += "Their name is: " + studentLookupResult.Name + ". Their email is: " + studentLookupResult.Email + ". Their username is: " + studentLookupResult.LoginId + ".";
                                                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasLookup", "[Whois]: " + requesterLookup + " asked who: " + lookupString + " is and was told: " + reply"));
                                            }
                                        }
                                        else
                                        {
                                            // student not found on Canvas
                                            reply += " But they were not found on Canvas for some reason.";
                                        }
                                    }
                                    else
                                    {
                                        // not found in our StudentsFile lookup either
                                        reply = "That user (" + lookupString + ") was not found in our records. Please check that you have typed it correctly. They may not have validated their Discord username.";
                                    }
                                }
                            }
                            else
                            {
                                // no user on the server matches that combo
                                reply = "That user (" + lookupString + ") doesn't exist on the server. Please check that you have typed it correctly.";
                            }

                        }
                        else if (lookupString.Length == 9)
                        {
                            // university or applicant id lookup
                            // should return discord username plus discriminator
                            int uniId = 0;
                            if (int.TryParse(lookupString, out uniId))
                            {
                                Applicant applicant = null;
                                if (ApplicantsFile.Instance.TryGetApplicant(uniId, out applicant))
                                {
                                    // this discord user matches one of the applicants
                                    reply = "That user (" + lookupString + ") is an applicant.";
                                    if (applicant.DiscordConnected)
                                    {
                                        SocketGuildUser discordUser = Program.GetSocketGuildUser(applicant.DiscordSnowflake);
                                        if(discordUser != null)
                                        {
                                            string usernamePlusDiscriminator = discordUser.Username + "#" + discordUser.Discriminator;
                                            reply += " Their current Discord handle on this server is: " + usernamePlusDiscriminator + ".";
                                        }
                                    }
                                }
                                else
                                {
                                    // they are not an applicant so check for them being a student.
                                    Student student = null;
                                    if (StudentsFile.Instance.TryGetStudent(uniId, out student))
                                    {
                                        // this discord user matches one of the students
                                        reply = "That user (" + lookupString + ") is a student.";
                                        SocketGuildUser discordUser = Program.GetSocketGuildUser(student.DiscordSnowflake);
                                        if (discordUser != null)
                                        {
                                            string usernamePlusDiscriminator = discordUser.Username + "#" + discordUser.Discriminator;
                                            reply += " Their current Discord handle on this server is: " + usernamePlusDiscriminator + ".";
                                            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasLookup", "[Whois]: " + requesterLookup + " asked who: " + lookupString + " is and was told: " + reply));
                                        }
                                        
                                    }
                                    else
                                    {
                                        //they are not a student either.
                                        reply = "That user (" + lookupString + ") doesn't exist on the server. Please check that you have typed it correctly.";
                                    }
                                }
                            }
                            else
                            {
                                //the provided string was not a number
                                reply = "The 9 character parameter that you provided was not a valid university id. Please check that you have typed it correctly.";
                            }
                        }
                        else if (lookupString.Length == 6)
                        {
                            // university username lookup
                            // should return discord username plus discriminator
                            reply = "Looking up a Discord user via their university login id is currently not implemented fully. Please use their 9 digit university or applicant id instead.";
                        }
                        else
                        {
                            // parameter not recognised
                            reply = "You attempted to use this command without a valid parameter: either a 9 digit applicant id, or a 9 digit university student id, or a Discord username and discriminator combo e.g. username#1234. The correct way to use this command is: !whois 123456789 or !whois username#1234";
                        }
                    }
                    else
                    {
                        // no parameter provided to command
                        reply = "You attempted to use this command without its required parameter: either a 9 digit applicant id, or a 9 digit university student id, or a Discord username and discriminator combo e.g. username#1234. The correct way to use this command is: !whois 123456789 or !whois username#1234";
                    }
                }
                else
                {
                    reply = "Please send me this request in a Direct Message (you can reply to this message if you like). You should delete your previous public message if you can.";
                }
            }

            Guid replyId = Program.AddMessageToQueue(requester, reply);
            _ = requester.GetOrCreateDMChannelAsync().ContinueWith(Program.SendMessage, replyId);            
            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "CanvasLookup", "[Whois]: " + requesterLookup + " asked who: " + lookupString + " is and was told: " + reply));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<IDMChannel> PackageBackchannel(IDMChannel channel)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return channel;
        }
    }
}
