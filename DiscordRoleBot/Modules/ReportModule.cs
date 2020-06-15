using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DiscordRoleBot.Modules
{
    public class ReportModule : ModuleBase<SocketCommandContext>
    {
        [Command("report")]
        [Summary("counts users")]
        public async Task ReportNumbers([Remainder] [Summary("Report summary of the user numbers")] string parameters = null)
        {
            string requesterLookup = Context.User.ToString();
            SocketGuildUser requester = Bot.GetSocketGuildUser(requesterLookup);
            SocketRole staffRole = Bot.GetRole("staff");
            string reply = "Something went wrong, not sure what.";
            parameters = parameters == null ? "applicants,students" : parameters.Trim();
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
                        reply = "Reporting: \n";
                        string[] parametersTokens = parameters.Split(',');
                        foreach(string parameter in parametersTokens)
                        {
                            string trimmedParameter = parameter.Trim();
                            if(trimmedParameter == "applicants")
                            {
                                int numberOfApplicants = ApplicantsFile.Instance.NumberOfRegisteredApplicants();
                                reply += "Number of " + trimmedParameter + ": " + numberOfApplicants + ".\n";

                            }
                            else if(trimmedParameter == "students")
                            {
                                int numberOfStudents = StudentsFile.Instance.NumberOfRegisteredStudents();
                                reply += "Number of " + trimmedParameter + ": " + numberOfStudents + ".\n";
                            }
                            else
                            {
                                // unrecognised
                                reply += "The parameter: " + trimmedParameter + " was not recognised.\n";
                            }
                        }
                    }
                }
                else
                {
                    reply = "Please send me this request in a Direct Message (you can reply to this message if you like). You should delete your previous public message if you can.";
                }
            }

            Bot.SendMessage(requester, reply);

            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "ReportModule", "[Report]: " + requesterLookup + " asked for: " + parameters + " and was told: " + reply));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<IDMChannel> PackageBackchannel(IDMChannel channel)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return channel;
        }
    }
}
