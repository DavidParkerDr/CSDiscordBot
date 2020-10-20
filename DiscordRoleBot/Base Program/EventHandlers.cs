using Discord;
using Discord.WebSocket;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    internal partial class Bot
    {
        /// <summary>
        /// Until the bot client is ready we can't do anything like getting the users from ids
        /// this handler will send a DM to the Notify id specified in the appconfig.json
        /// </summary>
        /// <returns></returns>
        private async Task<Task> ClientReadyEventHandler()
        {
            if (!commandsRegistered)
            {
                commandsRegistered = true;
                DiscordRoleBot.Services.Initialize i = new DiscordRoleBot.Services.Initialize(null, _client);
                DiscordRoleBot.Services.CommandHandler commandHandler = new DiscordRoleBot.Services.CommandHandler(_client, i.BuildServiceProvider());
                await commandHandler.InstallCommandsAsync();
            }

            Notify("I'm back baby!");

            string users = "DavidParkerDr#6742,JDixonHull#1878";

            if (!canvasThreadStarted)
            {
                canvasThreadStarted = true;
                Thread CanvasThread = new Thread(CanvasClient.Instance.Go);
                CanvasThread.Start();
            }
            return Task.CompletedTask;
        }

        private static Task ClientUserJoinedEventHandler(SocketGuildUser user)
        {
            string userType = "'unknown'";
            Student student = null;
            Applicant applicant = null;
            if (StudentsFile.Instance.TryGetDiscordStudent(user.Id, out student))
            {
                userType = "'existing student' (" + student.StudentId + ") joined the server.";
                SocketRole studentRole = GetRole("student");
                _ = AddRoleToUser(user, studentRole);
            }
            else if (ApplicantsFile.Instance.TryGetDiscordApplicant(user.Id, out applicant))
            {
                userType = "'applicant' (" + applicant.ApplicantId + ")";
                SocketRole applicantRole = GetRole("applicant");
                _ = AddRoleToUser(user, applicantRole);
            }
            else
            {
                string message = @"Welcome to the Computer Science and Technology Discord. If you are one of our students, please ensure that you have submitted your Username and ID at https://canvas.hull.ac.uk/courses/17835/quizzes/20659 which will give you permissions to use the server. Please note that your username is case sensitive. The process may take up to 2 hours to complete. If you are not yet one of our students, but have applied, then if you reply to this message with the following command, your user will be validated and you will gain access to the Applicant Zone on the server. The command is !applicant 123456789, where you replace that 9 digit number with the 9 digit application id that you were provided by the University. If you have any problems, please get in touch with John Dixon (JDixonHull#1878) or David Parker (DavidParkerDr#6742).";
                SendMessage(user, message);
            }


            string notification = "User " + user.Username + "#" + user.Discriminator + "(" + user.Id + ") of type" + userType + " joined the server.";
            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Bot", notification));
            Notify(notification);

            return Task.CompletedTask;
        }

        /// <summary>
        /// This handler will get called once the bot client has connected to the server.
        /// </summary>
        /// <returns></returns>
        private async Task<Task> ClientConnectedEventHandler()
        {
            // this code has been moved to ClientReady as there appears to be an issue assigning roles
            //if (!commandsRegistered)
            //{
            //    commandsRegistered = true;
            //    DiscordRoleBot.Services.Initialize i = new DiscordRoleBot.Services.Initialize(null, _client);
            //    DiscordRoleBot.Services.CommandHandler commandHandler = new DiscordRoleBot.Services.CommandHandler(_client, i.BuildServiceProvider());
            //    await commandHandler.InstallCommandsAsync();
            //}
            return Task.CompletedTask;
        }
    }
}
