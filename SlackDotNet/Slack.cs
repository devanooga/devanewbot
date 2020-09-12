using Flurl;
using Flurl.Http;
using SlackDotNet.Webhooks;
using SlackDotNet.Responses;
using SlackDotNet.Payloads;
using System.Threading.Tasks;

namespace SlackDotNet
{
    public class Slack
    {
        private string OauthToken { get; set; }
        private string SigningSecret { get; set; }
        private string VerificationToken { get; set; }

        public Slack(string oauthToken, string signingSecret, string verificationToken)
        {
            OauthToken = oauthToken;
            SigningSecret = signingSecret;
            VerificationToken = verificationToken;
        }

        /// <summary>
        /// Verifies the authenticity of a webhook message from Slack.
        /// Should be used before acting on a webhook.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool ValidWebhookMessage(WebhookMessage model)
        {
            return model.Token == VerificationToken;
        }

        /// <summary>
        /// Verifies the authenticity of an interactive message from Slack.
        /// Should be used before acting on an interactive message.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool ValidInteractiveMessage(InteractiveMessage model)
        {
            return model.Token == VerificationToken;
        }

        /// <summary>
        /// Get's a slack user's information
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<User> GetUser(string userId)
        {
            var response = await "https://slack.com/api/users.info"
                .SetQueryParam("token", OauthToken)
                .SetQueryParam("user", userId)
                .GetJsonAsync<Response>();
            
            return response.User;
        }

        /// <summary>
        /// Posts a message to a channel
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> PostMessage(ChatMessage message, bool ephemeral = false)
        {
            var endpoint = ephemeral ? "postEphemeral" : "postMessage";
            var response = await $"https://slack.com/api/chat.{endpoint}"
                .WithHeader("Authorization", "Bearer " + OauthToken)
                .PostJsonAsync(message);

            return true;
        }

        /// <summary>
        /// Deletes a message in response to an interactive command
        /// </summary>
        /// <param name="responseUrl"></param>
        /// <returns></returns>
        public async Task<bool> DeleteResponse(string responseUrl)
        {
            var response = await responseUrl
                .WithHeader("Authorization", "Bearer " + OauthToken)
                .PostJsonAsync(new
                {
                    delete_original = true
                });

            return true;
        }
    }
}