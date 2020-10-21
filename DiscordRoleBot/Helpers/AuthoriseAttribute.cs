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
    /// <summary>
    /// Marks a command as requiring a role or set of roles in order to run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthoriseAttribute : PreconditionAttribute
    {
        private readonly List<SocketRole> _roles = new List<SocketRole>();

        public AuthoriseAttribute(ulong roleId)
        {
            _roles.Add(Bot.GetRole(roleId));
            CheckRole();
        }

        public AuthoriseAttribute(string roleName)
        {
            _roles.Add(Bot.GetRole(roleName));
            CheckRole();
        }

        public AuthoriseAttribute(ulong[] roleIds)
        {
            foreach (ulong roleId in roleIds)
            {
                _roles.Add(Bot.GetRole(roleId));
            }
            CheckRole();
        }

        public AuthoriseAttribute(string[] roleNames)
        {
            foreach(string roleName in roleNames)
            {
                _roles.Add(Bot.GetRole(roleName));
            }
            CheckRole();
        }

        private void CheckRole()
        {
            if(_roles.Any(r => r == null))
            {
                _ = FileLogger.Instance.Log(new LogMessage(LogSeverity.Info, "Bot", "Precondition for a command has a role that doesn't exist!"));
                _roles.RemoveAll(r => r == null);
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

            if (requester != null)
            {
                if (_roles.Count == 0 || !Bot.GetGuild().Roles.Any(r => _roles.Any(_r => _r.Id == r.Id)))
                {
                    string reply = $"The server does not have the role(s) required to access this command or you are not part of the server";
                    Log(requester, reply);
                    return PreconditionResult.FromError(reply);
                }
                else
                {
                    if (requester.Roles.Any(r => _roles.Any(_r => _r.Id == r.Id)))
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
            else
            {
                string reply = $"User was not found on the current channel";
                Log(requester, reply);
                return PreconditionResult.FromError(reply);
            }
        }
    }
}
