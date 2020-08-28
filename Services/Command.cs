using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SlackDotNet;
using SlackDotNet.Webhooks;

namespace devanewbot.Services
{
    /// <summary>
    /// Abstract class for all /slashCommands
    /// </summary>
    public abstract class Command
    {
        public readonly Slack Slack;
        public readonly IConfiguration Configuration;
        public readonly Uri BotImageUrl = new Uri("https://ca.slack-edge.com/T3WU74872-USLACKBOT-sv41d8cd98f0-512");

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