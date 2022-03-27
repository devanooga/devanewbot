namespace devanewbot.Services
{
    using devanewbot.Models;
    using Microsoft.Extensions.Configuration;
    using SlackDotNet.Webhooks;
    using Flurl;
    using Flurl.Http;
    using System.Threading.Tasks;
    using SlackDotNet.Payloads;
    using SlackDotNet;
    using System.Collections.Generic;
    using System;
    using Microsoft.Extensions.Logging;

    public class GifCommand : Command
    {
        private Random Random = new Random();

        public GifCommand(Slack slack, IConfiguration configuration, ILogger<GifCommand> logger) : base("jif", slack, configuration, logger)
        {
        }

        private string SearchUri { get; set; } = "https://api.cognitive.microsoft.com/bing/v7.0/images/search";

        /// <summary>
        /// Sends a gif to Slack as the user that requested the gif.
        /// </summary>
        /// <param name="webhookMessage"></param>
        /// <seealso cref="Command.HandleMessage(WebhookMessage)"/>
        /// <returns></returns>
        protected override async Task HandleMessage(WebhookMessage webhookMessage)
        {
            var slackUser = await Slack.GetUser(webhookMessage.UserId);
            var url = await Response(webhookMessage);
            await SendGif(webhookMessage.ChannelId,
                slackUser.Profile.DisplayName,
                slackUser.Profile.ImageOriginal,
                webhookMessage.UserId,
                webhookMessage.Text,
                url);
        }

        public async Task<string> Response(WebhookMessage webhookMessage)
        {
            return await ImageSearch(webhookMessage.Text);
        }

        private async Task<string> ImageSearch(string searchTerm, bool random = false)
        {
            var uri = SearchUri.SetQueryParam("q", searchTerm)
                .SetQueryParam("imageType", "AnimatedGifHttps")
                .SetQueryParam("safeSearch", "Strict")
                .WithHeader("Ocp-Apim-Subscription-Key", Configuration["BingSearch:Key"]);

            var response = await uri.GetJsonAsync<ImageSearch>();

            var count = response.Value.Count;
            if (random)
            {
                // Get a random gif
                return response.Value[Random.Next(count)].ContentUrl.ToString();
            }

            // Grab the first gif
            return response.Value[0].ContentUrl.ToString();
        }

        /// <summary>
        /// Handles interactive messages. Used for button presses on previews.
        /// </summary>
        /// <param name="interactiveMessage"></param>
        /// <returns></returns>
        public override async Task HandleInteractive(InteractiveMessage interactiveMessage)
        {
            if (Slack.ValidInteractiveMessage(interactiveMessage))
            {
                var action = interactiveMessage.Actions[0];

                var slackUser = await Slack.GetUser(interactiveMessage.User.Id);
                if (action.Name == "post")
                {
                    await Slack.PostMessage(new ChatMessage
                    {
                        Channel = interactiveMessage.Channel.Id,
                        Username = slackUser.Profile.DisplayName,
                        Text = action.Value,
                        IconUrl = slackUser.Profile.ImageOriginal,
                        User = interactiveMessage.User.Id,
                    });
                }
                else if (action.Name == "random")
                {
                    var url = await ImageSearch(action.Value, true);
                    await SendGif(interactiveMessage.Channel.Id,
                        slackUser.Profile.DisplayName,
                        slackUser.Profile.ImageOriginal,
                        slackUser.Id,
                        action.Value,
                        url);
                }

                await Slack.DeleteResponse(interactiveMessage.ResponseUrl.ToString());
            }
        }

        /// <summary>
        /// Handles the work of actually sending the Gif ephemeral dialog
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="displayName"></param>
        /// <param name="iconUrl"></param>
        /// <param name="userId"></param>
        /// <param name="text"></param>
        /// <param name="gifUrl"></param>
        /// <returns></returns>
        private async Task SendGif(string channelId, string displayName, Uri iconUrl, string userId, string text, string gifUrl)
        {
            await Slack.PostMessage(new ChatMessage
            {
                Channel = channelId,
                Username = displayName,
                Text = "Can do! :cando:",
                IconUrl = iconUrl,
                User = userId,
                Attachments = new List<ChatAttachment>
                {
                    new ChatAttachment
                    {
                        Text = text,
                        CallbackId = "jif",
                        ImageUrl = gifUrl,
                        Actions = new List<ChatAction>()
                        {
                            new ChatAction
                            {
                                Type = "button",
                                Name = "post",
                                Text = "Post",
                                Value = gifUrl,
                                Style = "primary"
                            },
                            new ChatAction
                            {
                                Type = "button",
                                Name = "random",
                                Text = "Hit me baby one more time!",
                                Value = text,
                            },
                            new ChatAction
                            {
                                Type = "button",
                                Name = "cancel",
                                Text = "Nevermind",
                                Value = "cancel",
                                Style = "danger"
                            }
                        }
                    }
                }
            }, true);
        }
    }
}
