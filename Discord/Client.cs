namespace Devanewbot.Discord
{
    using System;
    using System.Threading.Tasks;
    using global::Discord.Rest;
    using global::Discord.WebSocket;
    using Microsoft.Extensions.Options;
    using SlackDotNet;
    using SlackDotNet.Payloads;

    public class Client : IDisposable
    {
        protected DiscordSocketClient SocketClient { get; }

        protected Slack Slack { get; }

        protected DiscordOptions DiscordOptions { get; }
        private static readonly string ChannelId = "C035Z122Z0F";

        public Client(Slack slack,
            IOptions<DiscordOptions> discordOptions)
        {
            SocketClient = new DiscordSocketClient();
            Slack = slack;
            DiscordOptions = discordOptions.Value;
        }

        public void Dispose()
        {
            SocketClient.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task Start()
        {

            SocketClient.UserVoiceStateUpdated += UserVoiceUpdated;
            await SocketClient.LoginAsync(global::Discord.TokenType.Bot, DiscordOptions.Token);
            await SocketClient.StartAsync();
        }

        public async Task UserVoiceUpdated(SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
        {
            if (user is not SocketGuildUser guildUser)
            {
                return;
            }

            var restUser = await SocketClient.Rest.GetGuildUserAsync(guildUser.Guild.Id, guildUser.Id);
            if (oldVoiceState.VoiceChannel == null && newVoiceState.VoiceChannel != null)
            {
                await SendMessage("joined", restUser, newVoiceState.VoiceChannel);
            }
            else if (oldVoiceState.VoiceChannel != null & newVoiceState.VoiceChannel == null)
            {
                await SendMessage("left", restUser, oldVoiceState.VoiceChannel);
            }
        }

        protected async Task SendMessage(string action, RestGuildUser user, SocketVoiceChannel channel)
        {
            var message = new ChatMessage()
            {
                Channel = ChannelId,
                Text = @$"{user.DisplayName} has {action} #{channel.Name}"
            };

            await Slack.PostMessage(message);
        }
    }
}
