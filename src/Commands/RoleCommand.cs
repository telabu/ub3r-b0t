﻿namespace UB3RB0T.Commands
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Net;
    using System.Text.RegularExpressions;
    using Discord;
    using Discord.Net;
    using Discord.WebSocket;

    [BotPermissions(GuildPermission.ManageRoles, "gee thanks asswad I can't manage roles in this server. not much I can do for ya here buddy. unless you wanna, y'know, up my permissions")]
    public class RoleCommand : IDiscordCommand
    {
        private static readonly Regex RoleIdRegex = new Regex("roleid:(?<roleid>[0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RoleMentionRegex = new Regex("<@&(?<roleid>[0-9]+)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private bool isAdd;

        public RoleCommand(bool isAdd)
        {
            this.isAdd = isAdd;
        }

        public async Task<CommandResponse> Process(IDiscordBotContext context)
        {
            if (context.GuildChannel != null)
            {
                var settings = SettingsConfig.GetSettings(context.GuildChannel.Guild.Id.ToString());
                var roleArgs = context.Message.Content.Split(new[] { ' ' }, 2);

                if (roleArgs.Length == 1)
                {
                    return new CommandResponse { Text = $"Usage: {settings.Prefix}role rolename | {settings.Prefix}derole rolename" };
                }

                IRole requestedRole = context.Message.MentionedRoles.FirstOrDefault();
                if (requestedRole == null)
                {
                    var guildRoles = context.GuildChannel.Guild.Roles.OrderBy(r => r.Position);
                    requestedRole = guildRoles.FirstOrDefault(r => r.Name.IEquals(roleArgs[1])) ?? 
                        guildRoles.FirstOrDefault(r => r.Name.IContains(roleArgs[1]));

                    if (requestedRole == null)
                    {
                        return new CommandResponse { Text = "wtf? role not found, spel teh name beter or something." };
                    }
                }

                if (!context.Settings.SelfRoles.Contains(requestedRole.Id))
                {
                    return new CommandResponse { Text = $"woah there buttmunch tryin' to cheat the system? you don't have the AUTHORITY to self-assign the {requestedRole.Name.ToUpperInvariant()} role. now make like a tree and get outta here" };
                }


                var guildAuthor = context.Message.Author as IGuildUser;
                if (isAdd && guildAuthor.RoleIds.Contains(requestedRole.Id))
                {
                    return new CommandResponse { Text = $"seriously? you already have the {requestedRole.Name} role. settle DOWN, freakin' role enthustiast" };
                }

                if (!isAdd && !guildAuthor.RoleIds.Contains(requestedRole.Id))
                {
                    return new CommandResponse { Text = $"seriously? you don't even have the {requestedRole.Name} role. settle DOWN, freakin' role unenthustiast" };
                }

                try
                {
                    if (isAdd)
                    {
                        await guildAuthor.AddRoleAsync(requestedRole);
                        return new CommandResponse { Text = $"access granted to role `{requestedRole.Name}`. congratulation !" };
                    }
                    else
                    {
                        await guildAuthor.RemoveRoleAsync(requestedRole);
                        return new CommandResponse { Text = $"access removed from role `{requestedRole.Name}`. congratulation ... ?" };
                    }
                }
                catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                {
                    return new CommandResponse { Text = "...it seems I cannot actually modify that role. yell at management (verify the role orders, bot's role needs to be above the ones being managed)" };
                }
            }

            return new CommandResponse { Text = "role command does not work in private channels" };
        }

        // TODO:
        // Share some of this logic with the command itself
        public static async Task<bool> AddRoleViaReaction(IUserMessage message, SocketReaction reaction)
        {
            // +/- reaction indicates an add/remove role request.
            if (reaction.Channel is SocketGuildChannel guildChannel)
            {
                // check for a mentioned role or roleid:### 
                string roleIdText = string.Empty;
                var roleMention = RoleMentionRegex.Match(message.Content);
                if (roleMention.Success)
                {
                    roleIdText = roleMention.Groups["roleid"].ToString();
                }
                else
                {
                    var roleIdMatch = RoleIdRegex.Match(message.Content);
                    if (roleIdMatch.Success)
                    {
                        roleIdText = roleIdMatch.Groups["roleid"].ToString();
                    }
                }
                
                if (ulong.TryParse(roleIdText, out var roleId))
                {
                    var settings = SettingsConfig.GetSettings(guildChannel.Guild.Id);
                    if (settings.SelfRoles.Contains(roleId))
                    {
                        var requestedRole = guildChannel.Guild.Roles.FirstOrDefault(r => r.Id == roleId);
                        if (requestedRole != null)
                        {
                            var guildAuthor = reaction.User.Value as IGuildUser;
                            try
                            {
                                if (reaction.Emote.Name == "➕")
                                {
                                    await guildAuthor.AddRoleAsync(requestedRole);
                                }
                                else
                                {
                                    await guildAuthor.RemoveRoleAsync(requestedRole);
                                }

                                return true;
                            }
                            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                            {
                                // ignore
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
