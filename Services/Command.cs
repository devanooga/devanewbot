using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SlackDotNet;
using SlackDotNet.Webhooks;

namespace devanewbot.Services
{
    public abstract class Command
    {
        public readonly Slack Slack;
        public readonly IConfiguration Configuration;

        public Command(Slack slack, IConfiguration configuration)
        {
            Slack = slack;
            Configuration = configuration;
        }

        protected abstract Task HandleMessage(WebhookMessage webhookMessage);

        public async Task<bool> ExecuteAsync(WebhookMessage webhookMessage)
        {
            if (Slack.ValidWebhookMessage(webhookMessage) && webhookMessage.UserId != null)
            {
                await HandleMessage(webhookMessage);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}