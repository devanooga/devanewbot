namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using SlackNet;
using SlackNet.Interaction;

public class SimpsonsCommand(ISlackApiClient client) : RandomCartoonCommand, ISlashCommandHandler
{
    private readonly Uri Frinkiac = new("https://frinkiac.com/");

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        await SendRandomCartoonImage(Frinkiac, command, client, command.Text);

        return null;
    }
}
