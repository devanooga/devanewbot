namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;

public class RemoveMessageShortcut(
    ISlackApiClient client,
    SlackDirectory directory,
    IOptions<SiteOptions> siteOptions) : IMessageShortcutHandler
{
    public const string CallbackId = "remove-message";

    protected SiteOptions Site { get; } = siteOptions.Value;

    public async Task Handle(MessageShortcut request)
    {
        await client.Views.Open(request.TriggerId, new ModalViewDefinition
        {
            Title = "Remove messages",
            Blocks = { new SectionBlock { Text = new Markdown(await Body(request)) } },
            Close = "Close"
        });
    }

    private async Task<string> Body(MessageShortcut request)
    {
        if (!await directory.IsAdmin(request.User.Id))
        {
            return "Only workspace admins can remove messages.";
        }

        if (request.Message.User is null)
        {
            return "Bot messages can't be removed from here.";
        }

        if (!Site.BaseUrlConfigured)
        {
            return "Site:BaseUrl is not set, so there is no admin panel to link to.";
        }

        var url = $"{Site.BaseUrl!.TrimEnd('/')}/admin/moderation/remove"
            + $"?user={Uri.EscapeDataString(request.Message.User)}"
            + $"&channel={Uri.EscapeDataString(request.Channel.Id)}"
            + $"&ts={Uri.EscapeDataString(request.Message.Ts)}";
        return $"*<{url}|Open the removal page>* to pick which of <@{request.Message.User}>'s recent messages to remove.";
    }
}
