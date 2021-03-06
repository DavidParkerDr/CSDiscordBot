﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRoleBot.Modules
{
    public class ApplicantModule : ModuleBase<SocketCommandContext>
    {
        [Command("applicant")]
        [Summary("validates new applicant against their applicant reference id and assigns applicant role")]
        public async Task AssignApplicantAsync([Remainder][Summary("The applicant id to verify")] string parameters = null)
        {
            string userLookup = Context.User.Username + "#" + Context.User.Discriminator;
            SocketGuildUser user = Bot.GetSocketGuildUser(userLookup);
            string reply = "Something went wrong, not sure what.";
            string applicantReferenceIdString = "";
            if (Context.IsPrivate)
            {
                if (parameters != null)
                {
                    applicantReferenceIdString = parameters.Trim();


                    if (applicantReferenceIdString.Length != 9)
                    {
                        // this is not an applicant id as it is not 9 digits long
                        reply = "The applicant id that you provided: " + applicantReferenceIdString + " is not a 9 digit number. The correct way to use this command is: !applicant 123456789 (where 123456789 should be replaced with your own applicant id)";
                    }
                    else
                    {
                        // 9 characters, could be id
                        int applicantReferenceId;
                        bool isNumber = int.TryParse(applicantReferenceIdString, out applicantReferenceId);
                        if (!isNumber)
                        {
                            // may have been 9 digits but was not an integer
                            reply = "The applicant id that you provided: " + applicantReferenceIdString + " is not a 9 digit number. The correct way to use this command is: !applicant 123456789 (where 123456789 should be replaced with your own applicant id)";
                        }
                        else
                        {
                            // it was 9 digits and successfully parsed as an int
                            // now we can check the db for the applicant id to
                            // verify
                            Applicant applicant = null;
                            bool isApplicant = ApplicantsFile.Instance.TryGetApplicant(applicantReferenceId, out applicant); // check DB
                            if (isApplicant)
                            {
                                bool snowflakeAdded = applicant.AddDiscordSnowflake(user.Id);
                                if (snowflakeAdded)
                                {
                                    // assignRole of applicant as they have supplied a valid id
                                    _ = Bot.AddRoleToUser(user, Bot.GetRole("applicant"));
                                    reply = "Thanks. Welcome to the Computer Science and Technology Discord Server. As an applicant you now have access to the Applicant Zone; check out the channels in there and feel free to talk amongst yourselves or ask us any questions that you like.";
                                    //add applicant to discordLookup list
                                    ApplicantsFile.Instance.UpdateDiscordLookup(applicant);

                                }
                                else
                                {
                                    reply = "The applicant id that you provided: " + applicantReferenceIdString + " did not work. Please check that you have supplied the right id. If you are sure, then please get in touch as something has gone wrong.)";
                                }
                            }
                            else
                            {
                                reply = "The applicant id that you provided: " + applicantReferenceIdString + " did not work. Please check that you have supplied the right id. If you are sure, then please get in touch as something has gone wrong.)";
                            }
                        }
                    }
                }
                else
                {
                    // no parameter provided to command
                    reply = "You attempted to use this command without its required parameter: your 9 digit applicant id. The correct way to use this command is: !applicant 123456789 (where 123456789 should be replaced with your own applicant id)";
                }
            }
            else
            {
                reply = "Please send me this request in a Direct Message (you can reply to this message if you like). You should delete your previous public message if you can.";
            }

            Bot.SendMessage(user, reply);

            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Bot", "[Applicant]: " + userLookup + " sent applicant id " + applicantReferenceIdString + " and was told: " + reply));
            string notification = "[Applicant]: " + userLookup + " sent applicant id " + applicantReferenceIdString + " and was told: " + reply;
            Bot.Notify(notification);
        }
    }        
}
