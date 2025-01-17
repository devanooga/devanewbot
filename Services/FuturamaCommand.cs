namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using SlackNet;
using SlackNet.Interaction;

public class FuturamaCommand(ISlackApiClient client) : RandomCartoonCommand, ISlashCommandHandler
{
    private readonly Uri Morbotron = new("https://morbotron.com");

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        await SendRandomCartoonImage(Morbotron, command, client);

        return null;
    }
}
