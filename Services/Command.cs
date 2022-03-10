namespace devanewbot.Services
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using SlackDotNet;
    using SlackDotNet.Webhooks;

    /// <summary>
    /// Abstract class for all /slashCommands
    /// </summary>
    public abstract class Command
    {
        public readonly Slack Slack;
        public readonly IConfiguration Configuration;

        public Command(Slack slack, IConfiguration configuration)
        {
            Slack = slack;
            Configuration = configuration;
        }

        /// <summary>
        /// This is called after the webhook is validated.
        /// 
        /// Any task that the command's Service needs to do should be done here.
        /// </summary>
        /// <param name="webhookMessage"></param>
        /// <returns></returns>
        protected abstract Task HandleMessage(WebhookMessage webhookMessage);

        /// <summary>
        /// Public interface for running command's task. Validation of messages happens inside this method.
        /// </summary>
        /// <param name="webhookMessage"></param>
        /// <returns></returns>
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
