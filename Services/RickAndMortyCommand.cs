namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using SlackNet;
using SlackNet.Interaction;

public class RickAndMortyCommand(ISlackApiClient client) : RandomCartoonCommand, ISlashCommandHandler
{
    private readonly Uri MasterOfAllScience = new("https://masterofallscience.com");

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        await SendRandomCartoonImage(MasterOfAllScience, command, client);

        return null;
    }
}
