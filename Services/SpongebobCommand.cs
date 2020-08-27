using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SlackDotNet;
using SlackDotNet.Payloads;
using SlackDotNet.Webhooks;

namespace devanewbot.Services
{
    public class SpongebobCommand : Command
    {
        private Random Random = new Random();

        public SpongebobCommand(Slack slack, IConfiguration configuration) : base(slack, configuration)
        {
        }

        /// <summary>
        /// Sends a spongebobified message to Slack as the user that requested.
        /// </summary>
        /// <param name="webhookMessage"></param>
        /// <seealso cref="Command.HandleMessage(WebhookMessage)"/>
        /// <returns></returns>
        protected override async Task HandleMessage(WebhookMessage webhookMessage)
        {
            var slackUser = await Slack.GetUser(webhookMessage.UserId);
            await Slack.PostMessage(new ChatMessage
            {
                Channel = webhookMessage.ChannelId,
                Username = slackUser.Profile.DisplayName,
                Text = Response(webhookMessage.Text),
                IconUrl = slackUser.Profile.ImageOriginal
            });
        }

        public string Response(string text)
        {
            // spONgEbOB cASe THE TexT
            text = new string(text.Select(c => Spongebobify(c)).ToArray());

            return $"{text} :spongebobmock:";
        }

        /// <summary>
        /// Takes char and randomly converts it to uppercase or lowercase
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private char Spongebobify(char c)
        {
            return Random.Next(2) == 0 ? Char.ToUpper(c) : Char.ToLower(c);
        }
    }
}