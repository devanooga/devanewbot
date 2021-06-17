namespace devanewbot.Services
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using SlackDotNet;
    using SlackDotNet.Payloads;

    public class PropagationService
    {
        public readonly Slack Slack;

        public PropagationService(Slack slack)
        {
            Slack = slack;
        }

        private const string Report = "http://www.hamqsl.com/solar101vhf.php";

        private const string ChannelId = "C3XHU6W9H";

        public async Task PostMessage()
        {
            var message = new ChatMessage()
            {
                Channel = ChannelId,
                Text = "Propagation Report",
                Attachments = new List<ChatAttachment>
                {
                    new ChatAttachment
                    {
                        ImageUrl = Report
                    }
                }
            };

            await Slack.PostMessage(message);
        }
    }
}