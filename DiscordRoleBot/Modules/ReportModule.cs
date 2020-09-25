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
        [Helpers.Authorise("staff")]
        public async Task ReportNumbers([Remainder] [Summary("Report summary of the user numbers")] string parameters = null)
        {
            string requesterLookup = Context.User.ToString();
            SocketGuildUser requester = Bot.GetSocketGuildUser(requesterLookup);
            string reply = "Something went wrong, not sure what.";
            parameters = parameters == null ? "applicants,students" : parameters.Trim();

            if (Context.IsPrivate)
            {
                if (parameters != null)
                {
                    reply = "Reporting: \n";
                    string[] parametersTokens = parameters.Split(',');
                    foreach (string parameter in parametersTokens)
                    {
                        string trimmedParameter = parameter.Trim();
                        if (trimmedParameter == "applicants")
                        {
                            int numberOfApplicants = ApplicantsFile.Instance.NumberOfRegisteredApplicants();
                            reply += "Number of " + trimmedParameter + ": " + numberOfApplicants + ".\n";

                        }
                        else if (trimmedParameter == "students")
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


            Bot.SendMessage(requester, reply);

            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "ReportModule", "[Report]: " + requesterLookup + " asked for: " + parameters + " and was told: " + reply));
        }

    }
}
