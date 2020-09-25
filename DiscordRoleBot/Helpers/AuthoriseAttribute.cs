using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRoleBot.Helpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AuthoriseAttribute : PreconditionAttribute
    {
        private readonly SocketRole _role;

        public AuthoriseAttribute(ulong roleId)
        {
            _role = Bot.GetRole(roleId);
        }

        public AuthoriseAttribute(string roleName)
        {
            _role = Bot.GetRole(roleName);
        }

        private void CheckRole()
        {
            if(_role == null)
            {
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Bot", "Precondition for a command is a role that doesn't exist!"));
            }
        }

        private void Log(SocketGuildUser requester, string message)
        {
            string requesterLookup = requester.Username + "#" + requester.Discriminator + " (" + requester.Nickname + ")";
            _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Authentication", "[Authenticated Command Module]: " + requesterLookup + " was told: " + message));
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            string userLookup = context.User.ToString();
            SocketGuildUser requester = Bot.GetSocketGuildUser(userLookup);

            if (_role == null)
            {
                string reply = $"The server does not have the role required to access this command or you are not part of the server";
                Log(requester, reply);
                PreconditionResult.FromError(reply);
            }
            
            if (requester.Roles.Contains(_role))
            {
                return PreconditionResult.FromSuccess();
            }
            else
            {
                string reply = $"You do not have the necessary privileges to perform that action.";
                Log(requester, reply);
                return PreconditionResult.FromError(reply);
            }
        }
    }
}
