using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using SlackDotNet.Webhooks;
using Flurl;
using Flurl.Http;
using System.Threading.Tasks;

namespace devanewbot.Models.Commands
{
    public class GifCommand : SlashCommand
    {
        private string SearchUri { get; set; } = "https://api.cognitive.microsoft.com/bing/v7.0/images/search";

        public async Task<string> Response()
        {
            return await ImageSearch(Text);
        }

        private async Task<string> ImageSearch(string searchTerm)
        {
            var uri = SearchUri.SetQueryParam("q", searchTerm)
                .SetQueryParam("imageType", "AnimatedGifHttps")
                .SetQueryParam("safeSearch", "Strict")
                .WithHeader("Ocp-Apim-Subscription-Key", Configuration["BingSearch:Key"]);

            var response = await uri.GetJsonAsync();

            // Grab the first gif
            return response.value[0].contentUrl;
        }
    }
}